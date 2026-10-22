"""Locally authored, weighted wardrobe pass; reads preserved Unity anatomy exports.
Writes only this implementation's Art directory. No provider or network calls.
"""
from pathlib import Path
import sys, math, json, hashlib
HERE=Path(__file__).resolve()
PROJECT=HERE.parents[2]
source=(HERE.parent/'create_tailored_characters.py').read_text()
source=source[:source.index('selection=sys.argv')]
# Ease the curator mantle over the shoulders and use interpolated rig weights.
source=source.replace("[(neck-.25,.305,.19),(neck-.13,.29,.18),(neck-.015,.19,.115),(neck+.015,.09,.083)]", "[(neck-.43,.255,.175),(neck-.31,.275,.175),(neck-.17,.265,.155),(neck-.045,.19,.115),(neck+.012,.091,.083)]")
source=source.replace("[(neck-.25,.308,.193),(neck-.235,.307,.192)]", "[(neck-.43,.258,.178),(neck-.415,.258,.178)]")
source=source.replace("(.018,.025,.032) if mystic", "(.055,.075,.082) if mystic")
source=source.replace('dist=.00005','dist=.004')
source=source.replace('scene.render.threads=4','scene.render.threads=2')
source=source.replace('scene.render.resolution_x=1100;scene.render.resolution_y=1300','scene.render.resolution_x=750 if "--quick" in sys.argv else 1100;scene.render.resolution_y=900 if "--quick" in sys.argv else 1300')
source=source.replace("            for o in PARTS[-2:]:\n                o.vertex_groups.clear();g=o.vertex_groups.new(name='Bip01 Spine2');g.add(list(range(len(o.data.vertices))),1,'REPLACE')", "            # Mantle retains barycentrically transferred shoulder/spine weights.")
# Keep Kai's authored native hair silhouette and scalp; the former replacement
# exposed a white strip and protruding tubular locks in neutral-light review.
source=source.replace("kai_scalp=hero and '_head'", "kai_scalp=False and hero and '_head'")
source=source.replace("(hero and 'opacity'", "(False and hero and 'opacity'")
hair_start=source.index("        scalp=[(fi,tri)");hair_end=source.index("        scar=material",hair_start)
source=source[:hair_start]+source[hair_end:]
source=source.replace("],ivory if hero else trim)","],leather if hero else trim)")
source=source.replace("node.image=tex", "node.image=bpy.data.images.load(str(DEST/(name+'.png')),check_existing=False)")
source=source.replace("shell('Split tailored coat tails',[(hip-.23,width+.045,depth+.035),(hip-.12,width+.03,depth+.020),(hip+.04,width+.01,depth),(hip+.12,width,depth)],plain,split=.035 if hero else .065,pleats=.0025)","draped_skirt('Fitted split explorer coat',hip-.23,hip+.12,plain,split=.035 if hero else .065)")
source=source.replace("shell('Woven waist sash' if hero else 'Waistcoat belt',[(hip+.09,width+.014,depth+.014),(hip+.13,width+.012,depth+.012)],leather if hero else trim)","conformal_band('Fitted explorer waist belt',hip+.09,hip+.13,leather if hero else trim)")
exec(compile(source,str(HERE.parent/'create_tailored_characters.py'),'exec'),globals())
# Reuse the original exact-anatomy preparation without its outfit design.
prefix=source[source.index('def create(character,style):'):source.index("    hip=BONES['Bip01 Pelvis'].z;neck=BONES['Bip01 Neck'].z",source.index('def create(character,style):'))]
exec(prefix.replace('def create(character,style):','def prepare(character,style):'),globals())

def waist_points(z,ease=.006):
    result=[]
    for i in range(64):
        # Begin inside the A-pose wrists: an outside ray can hit a hand/forearm
        # at waist height and incorrectly expand a skirt toward the arms.
        t=i*math.tau/64;axis=Vector((math.cos(t),math.sin(t),0));hit=BVH.ray_cast(axis*.29+Vector((0,0,z)),-axis,.55)[0]
        radius=max(.08,min(.245,hit.dot(axis))) if hit else .17
        result.append((axis,radius+ease))
    return result

def conformal_band(name,bottom,top,mat):
    vv=[];ff=[]
    for z in [bottom,top]:
        for axis,radius in waist_points(z,.013):p=axis*radius;vv.append((p.x,p.y,z))
    for i in range(64):b=(i+1)%64;ff.append((i,b,b+64,i+64))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vv,[],ff);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return finish(o,name,mat)

def draped_skirt(name,bottom,top,mat,split=0,hem=None):
    base=waist_points(top,.008);vv=[];ff=[]
    for row in range(9):
        progress=row/8;z=bottom+(top-bottom)*progress
        for i,(axis,radius) in enumerate(base):
            ease=.038*(1-progress)**1.5+.0025*math.sin(i*math.tau/64*12)*(1-progress)
            p=axis*(radius+ease);vv.append((p.x,p.y,z))
    for row in range(8):
        for i in range(64):
            t=(i+.5)*math.tau/64
            if split and row<7 and math.sin(t)<-.7 and abs(math.cos(t))*base[i][1]<split:continue
            a=row*64+i;b=row*64+(i+1)%64;ff.append((a,b,b+64,a+64))
    if max(abs(v[0]) for v in vv)>.29:raise ValueError('Garment ease exceeds torso bounds: '+name)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vv,[],ff);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);finish(o,name,mat)
    if hem:
        mesh.materials.append(hem)
        for face in mesh.polygons:
            if face.center.z<bottom+.018:face.material_index=1
    return o

def components(data,points):
    groups=[]
    for sub,s in enumerate(data['surfaces']):
        adj={}
        for k in range(0,len(s['triangles']),3):
            tri=s['triangles'][k:k+3]
            for i in tri:adj.setdefault(i,set()).update(tri)
        unseen=set(adj)
        while unseen:
            stack=[unseen.pop()];g=set(stack)
            while stack:
                for n in adj[stack.pop()]&unseen:unseen.remove(n);g.add(n);stack.append(n)
            groups.append((sub,g))
    return groups

def refitted_shirt_and_legs(style,cloth,pants,leather):
    # The clean shirt has no hood/apron. Retarget each rest vertex through its
    # weighted bone anchors, retaining four native weights and continuous UVs.
    src=json.loads((OUT/'Reference/Marcus.json').read_text())
    points=[v3(p) for p in src['vertices']]
    old={b['name']:v3(b['position']) for b in src['skeleton']}
    mapping={i:BONE_IDS[b['name']] for i,b in enumerate(src['skeleton'])}
    selection={}
    for sub,g in components(src,points):
        if '_body' not in src['surfaces'][sub]['name']:continue
        hand=sum(sum(w for b,w in zip(src['weights'][i]['bones'],src['weights'][i]['values']) if any(t in src['skeleton'][b]['name'] for t in (' Hand','Finger'))) for i in g)/len(g)
        lo=min(points[i].z for i in g);hi=max(points[i].z for i in g)
        if hand>.40:continue
        if style!='CafeHost' and lo>1.38 and len(g)<100:continue
        kind='shirt' if lo>.70 and hi>1.1 else 'shoe' if hi<.20 else 'legs' if hi<1.06 else None
        if kind:
            for i in g:selection[i]=kind
    transformed=[]
    for i,pt in enumerate(points):
        offset=Vector((0,0,0));w=src['weights'][i]
        for b,weight in zip(w['bones'],w['values']):
            name=src['skeleton'][b]['name'];offset+=(BONES[name]-old[name])*weight
        q=pt+offset
        # Female companion has a softer waist; skin/face/hands stay native.
        if style=='CafeHost' and abs(q.x)<.20 and .90<q.z<1.36:q.x*=.90
        transformed.append(q)
    result=[]
    for kind,mat in [('shirt',cloth),('legs',pants),('shoe',leather)]:
        tris=[]
        for sub,surf in enumerate(src['surfaces']):
            for k in range(0,len(surf['triangles']),3):
                t=surf['triangles'][k:k+3]
                if selection.get(t[0])==kind:tris.append(t)
        ids=sorted({i for t in tris for i in t});remap={i:n for n,i in enumerate(ids)}
        mesh=bpy.data.meshes.new(style+' continuous '+kind);mesh.from_pydata([transformed[i] for i in ids],[],[[remap[i] for i in t] for t in tris]);mesh.update()
        o=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(o)
        for b in DATA['skeleton']:o.vertex_groups.new(name=b['name'])
        for i,n in remap.items():
            for b,w in zip(src['weights'][i]['bones'],src['weights'][i]['values']):
                if w>0:o.vertex_groups[mapping[b]].add([n],w,'REPLACE')
        uv=mesh.uv_layers.new()
        for face,tri in zip(mesh.polygons,tris):
            for li,i in zip(face.loop_indices,tri):uv.data[li].uv=(src['uv'][i]['x'],src['uv'][i]['y'])
        for v in mesh.vertices:v.co+=v.normal*.004
        finish(o,mesh.name,mat,subdivide=True);result.append(o)
    return result

def custom(character,style):
    global BVH,transfer
    prepare(character,style)
    hip=BONES['Bip01 Pelvis'].z;neck=BONES['Bip01 Neck'].z;eye=BONES['Bip01 REye'].z
    companion=style=='CafeHost';guard=style=='RomanGuard';curator=style=='MuseumMystic'
    cloth=tinted(style+' woven cloth',(.14,.26,.23) if companion else (.035,.075,.095) if curator else (.57,.45,.29),rough=.86)
    trim=tinted(style+' woven edging',(.07,.12,.14) if companion else (.04,.19,.20) if curator else (.28,.045,.028),rough=.8)
    pants=tinted(style+' lower cloth',(.10,.14,.17) if companion else (.025,.035,.042) if curator else (.24,.20,.14),rough=.88)
    leather=tinted(style+' soft leather',(.055,.037,.024),rough=.72)
    metal=tinted(style+' brushed bronze',(.30,.22,.12),rough=.54,metal=.6)
    keep=set()
    for sub,g in components(DATA,POINTS):
        if '_body' not in DATA['surfaces'][sub]['name']:continue
        hand=sum(sum(w for b,w in zip(DATA['weights'][i]['bones'],DATA['weights'][i]['values']) if any(t in DATA['skeleton'][b]['name'] for t in (' Hand','Finger'))) for i in g)/len(g)
        if hand>.50:keep.update(g)
    for tri,(sub,ordinal) in zip(FACES,indices_global()):
        body='_body' in DATA['surfaces'][sub]['name']
        uv=[DATA['uv'][i] for i in tri]
        # Exact chef-cap UV islands, including its white rim. Keep native bob.
        centre_u=sum(v['x'] for v in uv)/3;centre_v=sum(v['y'] for v in uv)/3
        hat=companion and not body and (centre_v>.80 or (centre_u>.60 and centre_v<.33))
        if (guard or curator) and not body:
            z=sum(POINTS[i].z for i in tri)/3
            hat=hat or 'opacity' in DATA['surfaces'][sub]['name'] or z>eye+.083
        nativeHand=tri[0] in keep
        if nativeHand:
            centre=sum((POINTS[i] for i in tri),Vector())/3
            side=min(('L','R'),key=lambda side:(centre-BONES['Bip01 '+side+' Hand']).length_squared);hand=BONES['Bip01 '+side+' Hand'];forearm=BONES['Bip01 '+side+' Forearm']
            nativeHand=(centre-hand).dot((hand-forearm).normalized())>-.006
        if (body and not nativeHand) or hat:COVERAGE.append({'submesh':sub,'triangle':ordinal})
    fitted=refitted_shirt_and_legs(style,cloth,pants,leather)
    # Project details onto the new garment, rather than the discarded hoodie/apron.
    verts=[];faces=[];newweights=[]
    for o in fitted:
        off=len(verts);verts.extend(v.co.copy() for v in o.data.vertices);faces.extend(tuple(off+i for i in f.vertices) for f in o.data.polygons)
        for v in o.data.vertices:newweights.append({BONE_IDS[o.vertex_groups[g.group].name]:g.weight for g in v.groups})
    BVH=BVHTree.FromPolygons(verts,faces,all_triangles=False)
    def fitted_transfer(o):
        for b in DATA['skeleton']:o.vertex_groups.new(name=b['name'])
        # Match the BVH face indices to the same newly fitted polygons. Mixing
        # these with the old native face table produces invalid skirt weights.
        for v in o.data.vertices:
            p=o.matrix_world@v.co;hit,normal,face,dist=BVH.find_nearest(p)
            poly=faces[face];nearest=sorted(poly,key=lambda i:(verts[i]-hit).length_squared)[:3]
            factors=[1/max(.0001,(verts[i]-hit).length) for i in nearest];mix={}
            for i,f in zip(nearest,factors):
                for b,w in newweights[i].items():mix[b]=mix.get(b,0)+w*f
            best=sorted(mix.items(),key=lambda pair:pair[1],reverse=True)[:4];total=sum(w for b,w in best)
            for b,w in best:o.vertex_groups[b].add([v.index],w/total,'REPLACE')
    transfer=fitted_transfer
    if companion:
        # Keep the original bob silhouette, close only the removed cap's crown.
        hair=tinted('Maya natural dark bob crown',(.010,.007,.005),rough=.94)
        hair.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.12
        vv=[];ff=[]
        for j in range(13):
            theta=j/12*math.pi/2
            for i in range(64):
                angle=i*math.tau/64;st=math.sin(theta)
                vv.append((.089*st*math.cos(angle),.040+.108*st*math.sin(angle),eye-.030+.135*math.cos(theta)+.025*max(0,-math.sin(angle))*st))
        for j in range(12):
            for i in range(64):a=j*64+i;b=j*64+(i+1)%64;ff.append((a,a+64,b+64,b))
        mesh=bpy.data.meshes.new('Fitted native bob crown');mesh.from_pydata(vv,[],ff);mesh.update()
        crown=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(crown);finish(crown,crown.name,hair,'Bip01 Head')
        for side in (-1,1):ribbon('Cardigan shaped front edge',[(side*.040,neck-.06),(side*.047,neck-.21),(side*.038,hip+.13)],.012,trim)
        for z in (hip+.18,hip+.26,hip+.34):ellipsoid('Small stitched cardigan button',front(.026,z,.012),(.004,.003,.004),metal)
    elif curator:
        # A tailored curator coat replaces the previous rigid shoulder mantle.
        draped_skirt('Fitted split curator coat',hip-.23,hip+.10,cloth,split=.055)
        for side in (-1,1):ribbon('Curved teal coat lapel',[(side*.045,neck-.05),(side*.092,neck-.16),(side*.034,hip+.17)],.020,trim)
        top=eye+.14;head='Bip01 Head'
        for name,levels,mat in [('Restrained top hat brim',[(top-.025,.14,.125),(top-.015,.142,.127)],leather),('Shaped top hat crown',[(top-.02,.101,.094),(top+.07,.094,.088),(top+.15,.106,.097)],cloth),('Muted teal hat ribbon',[(top-.012,.102,.095),(top+.020,.100,.094)],trim)]:
            o=shell(name,levels,mat);o.vertex_groups.clear();g=o.vertex_groups.new(name=head);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
        ellipsoid('Closed cloth hat top',(0,0,top+.15),(.106,.097,.006),cloth,head)
        for z in (hip+.20,hip+.27,hip+.34):ellipsoid('Curator coat button',front(.025,z,.015),(.004,.003,.004),metal)
    else:
        draped_skirt('Draped knee length tunic',hip-.35,hip+.11,cloth,hem=trim)
        # Narrow belt follows the fitted waist; no broad shoulder or waist plate.
        conformal_band('Fitted soft leather belt',hip+.09,hip+.135,leather)
        pts=[(.10-.20*i/24,neck-.10-(neck-hip-.22)*i/24) for i in range(25)]
        ribbon('Draped diagonal woven sash',pts,.038,trim)
        if guard:
            head='Bip01 Head'
            helm=shell('Open faced bronze helmet',[(eye+.068,.125,.121),(eye+.105,.120,.117),(eye+.147,.100,.098),(eye+.175,.06,.06),(eye+.188,.001,.001)],metal)
            helm.vertex_groups.clear();g=helm.vertex_groups.new(name=head);g.add(list(range(len(helm.data.vertices))),1,'REPLACE')
            for side in (-1,1):curve('Narrow helmet side piece',[(side*.105,-.015,eye+.063),(side*.112,-.024,eye-.008),(side*.10,-.018,eye-.065)],.009,metal,head)
    hidden={(c['submesh'],c['triangle']) for c in COVERAGE}
    bm=bmesh.new();bm.from_mesh(BODY.data);bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i,pair in enumerate(indices_global()) if pair in hidden],context='FACES');bm.to_mesh(BODY.data);bm.free()
    result=export(style);render(style)
    receipt={'style':style,'character':character,'triangles':sum(len(p['triangles'])//3 for p in result['parts']),'coveredSourceTriangles':len(COVERAGE),'bones':len(BONE_IDS),'license':'Original authored garments; MIT Rocketbox-derived fitted anatomy','facialSource':'Original facial blendshapes retained by Unity builder','sha256':hashlib.sha256((DEST/(style+'.json')).read_bytes()).hexdigest()}
    (DEST/'receipt.json').write_text(json.dumps(receipt,indent=2));print('FINAL_CAST_READY',json.dumps(receipt),flush=True)

def indices_global():
    return [(sub,i//3) for sub,s in enumerate(DATA['surfaces']) for i in range(0,len(s['triangles']),3)]

selection=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for char,style in [('Kai','OpeningHero'),('Theo','Professor')]:
    if style in selection:create(char,style)
for char,style in [('Marcus','RomanCitizen'),('Orion','RomanGuard'),('Maya','CafeHost'),('Orion','MuseumMystic')]:
    if style in selection:custom(char,style)

"""Source-preserving, anatomically fitted garments and accessories for Lumen hosts.

Reads exact Unity rest anatomy/skin weights; renders in Blender and emits a
portable GLB plus explicit weighted geometry for lossless Unity rig admission.
All generated files stay on D. Original Rocketbox facial geometry is untouched.
"""
import bpy, bmesh, json, math, sys, hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=Path('D:/LumenProject/Art/CharacterTailoring');OUT.mkdir(parents=True,exist_ok=True)

def v3(p):return Vector((p['x'],-p['z'],p['y']))
def xyz(p):return {'x':p.x,'y':p.z,'z':-p.y}
def reset():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for block in [bpy.data.meshes,bpy.data.materials,bpy.data.images]:
        for item in list(block):
            if item.users==0:block.remove(item)

def material(name,color,rough=.65,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    m.diffuse_color=(*color,1)
    return m

def tinted(name,color,source=None,rough=.7,metal=0):
    m=material(name,color,rough,metal)
    if source:
        im=bpy.data.images.load(str(source),check_existing=True);w,h=im.size
        pixels=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(pixels);pixels=pixels.reshape((h,w,4))
        lum=(pixels[:,:,:3]*np.array([.2126,.7152,.0722])).sum(2)
        value=np.clip(np.power(np.maximum(lum,0)/.72,.65),.22,1.12)
        pixels[:,:,:3]=np.clip(value[:,:,None]*np.array(color),0,1);pixels[:,:,3]=1
        tex=bpy.data.images.new(name+' textile',w,h,alpha=False);tex.pixels.foreach_set(pixels.reshape(-1))
    else:
        w=h=512;y,x=np.mgrid[:h,:w]
        weave=.92+.045*np.sin(x*math.pi)+.035*np.cos(y*math.pi)+.02*np.sin(x*.31+y*.13)
        pixels=np.ones((h,w,4),dtype=np.float32);pixels[:,:,:3]=np.array(color)*weave[:,:,None]
        tex=bpy.data.images.new(name+' weave',w,h,alpha=False);tex.pixels.foreach_set(pixels.reshape(-1))
    # Store actual sRGB bytes; generated image buffers do not apply this
    # conversion automatically when saving the analytically authored colors.
    pixels[:,:,:3]=np.where(pixels[:,:,:3]<=.0031308,pixels[:,:,:3]*12.92,1.055*np.maximum(pixels[:,:,:3],0)**(1/2.4)-.055)
    tex.colorspace_settings.name='sRGB';tex.pixels.foreach_set(pixels.reshape(-1))
    tex.filepath_raw=str(DEST/(name+'.png'));tex.file_format='PNG';tex.save()
    node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=tex
    m.node_tree.links.new(node.outputs['Color'],m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    m['color_file']=name+'.png';m['roughness']=rough;m['metallic']=metal
    return m

def finish(o,name,mat,bone=None,subdivide=False):
    o.name=name
    if mat:o.data.materials.clear();o.data.materials.append(mat)
    for poly in o.data.polygons:poly.use_smooth=True
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    if subdivide:
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00005)
        bm.to_mesh(o.data);bm.free()
        mod=o.modifiers.new('Tailored smooth surface','SUBSURF');mod.levels=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if bone is not None:
        g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
    elif len(o.vertex_groups)==0:transfer(o)
    if len(o.data.uv_layers)==0:
        uv=o.data.uv_layers.new(name='UVMap')
        for poly in o.data.polygons:
            for li in poly.loop_indices:
                p=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=((p.x+.8)*2,(p.z+.1)*2)
    PARTS.append(o)
    o.select_set(False)
    return o

def transfer(o):
    # Barycentric interpolation on the exact native skinned reference surface.
    # New coat hems can fall below the original jacket and borrow nearby leg/hip weights.
    for b in DATA['skeleton']:o.vertex_groups.new(name=b['name'])
    for vertex in o.data.vertices:
        p=o.matrix_world@vertex.co;hit,normal,face,dist=BVH.find_nearest(p)
        indices=FACES[face] if face is not None else (0,0,0)
        a,b,c=[POINTS[i] for i in indices]
        v0=b-a;v1=c-a;v2=hit-a
        d00=v0.dot(v0);d01=v0.dot(v1);d11=v1.dot(v1);d20=v2.dot(v0);d21=v2.dot(v1)
        den=d00*d11-d01*d01
        q=(d11*d20-d01*d21)/den if abs(den)>1e-12 else 0
        r=(d00*d21-d01*d20)/den if abs(den)>1e-12 else 0
        mix={}
        for idx,factor in zip(indices,[max(0,1-q-r),max(0,q),max(0,r)]):
            for bone,weight in zip(DATA['weights'][idx]['bones'],DATA['weights'][idx]['values']):
                mix[bone]=mix.get(bone,0)+weight*factor
        best=sorted(mix.items(),key=lambda x:x[1],reverse=True)[:4];total=sum(w for _,w in best)
        for bone,weight in best:
            if weight>1e-5:o.vertex_groups[bone].add([vertex.index],weight/total,'REPLACE')

def surface(name,chosen,mat,inflate=.007,smooth=True):
    ids=sorted(set(i for _,tri in chosen for i in tri));mapping={v:i for i,v in enumerate(ids)}
    mesh=bpy.data.meshes.new(name);mesh.from_pydata([POINTS[i] for i in ids],[],[[mapping[i] for i in tri] for _,tri in chosen]);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    o['native_uv']=True
    for bone in DATA['skeleton']:o.vertex_groups.new(name=bone['name'])
    for source,index in mapping.items():
        w=DATA['weights'][source]
        for bone,weight in zip(w['bones'],w['values']):
            if weight>0:o.vertex_groups[bone].add([index],weight,'REPLACE')
    uv=mesh.uv_layers.new()
    for poly,(_,tri) in zip(mesh.polygons,chosen):
        for li,source in zip(poly.loop_indices,tri):
            uv.data[li].uv=(DATA['uv'][source]['x'],DATA['uv'][source]['y'])
    # The Unity-root to Blender axis conversion is a rotation (determinant+1),
    # so retain native triangle winding; reversing it inflates clothing inward.
    mesh.update()
    for vertex in mesh.vertices:vertex.co+=vertex.normal*inflate
    if 'boot uppers' in name:
        bm=bmesh.new();bm.from_mesh(mesh)
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,.36),plane_no=(0,0,1),clear_outer=True,clear_inner=False)
        bm.to_mesh(mesh);bm.free()
    if DATA['character']=='Theo' and 'jacket' in name:
        waist=BONES['Bip01 Pelvis'].z+.14
        for vertex in mesh.vertices:
            if vertex.co.z<waist and abs(vertex.co.x)<.24:
                factor=(waist-vertex.co.z)/.20
                vertex.co.z-=.075*factor;vertex.co.x*=1+.045*factor;vertex.co.y*=1+.08*factor
    return finish(o,name,mat,subdivide=smooth)

def shell(name,levels,mat,split=0,offset=(0,0,0),pleats=0):
    verts=[];faces=[];n=64
    for z,rx,ry in levels:
        for i in range(n):
            t=i*math.tau/n;fold=pleats*math.sin(t*12)
            verts.append((offset[0]+(rx+fold)*math.cos(t),offset[1]+(ry+fold)*math.sin(t),z))
    for j in range(len(levels)-1):
        for i in range(n):
            t=(i+.5)*math.tau/n
            if split and j<len(levels)-2 and math.sin(t)<-.6 and abs(math.cos(t))*levels[j][1]<split:continue
            a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat)

def curve(name,points,radius,mat,bone=None):
    cr=bpy.data.curves.new(name,'CURVE');cr.dimensions='3D';cr.bevel_depth=radius;cr.bevel_resolution=2;cr.resolution_u=8
    s=cr.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for i,(p,co) in enumerate(zip(s.bezier_points,points)):
        p.co=co;p.handle_left_type=p.handle_right_type='AUTO'
        if 'hair lock' in name:p.radius=[1,.60,.02][min(i,2)]
    o=bpy.data.objects.new(name,cr);bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False)
    return finish(o,name,mat,bone)

def ellipsoid(name,pos,size,mat,bone=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=pos);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,bone)

def front(x,z,extra=.012):
    hit=BVH.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))[0]
    return (x,hit.y-extra if hit else -.14,z)

def ribbon(name,points,width,mat):
    vertices=[]
    for x,z in points:
        vertices.extend([front(x-width/2,z,.040),front(x+width/2,z,.040)])
    faces=[(i+2,i+3,i+1,i) for i in range(0,len(vertices)-2,2)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat)

def export(style):
    result={'style':style,'character':DATA['character'],'parts':[],'covered':COVERAGE,'source':'MIT Microsoft Rocketbox anatomy with original Blender tailored garments'}
    for o in PARTS:
        # Export loop vertices to preserve seams and normals at hard accessories.
        bpy.context.view_layer.objects.active=o
        tri=o.modifiers.new('Runtime triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
        mesh=o.data;mesh.update();entries=[];material_indices={};uv=mesh.uv_layers.active
        for poly in mesh.polygons:
            local=[]
            for li in poly.loop_indices:
                loop=mesh.loops[li];vertex=mesh.vertices[loop.vertex_index];p=o.matrix_world@vertex.co
                normal=o.matrix_world.to_3x3()@loop.normal
                groups=sorted([(g.group,g.weight) for g in vertex.groups],key=lambda x:x[1],reverse=True)[:4]
                weights=[0.0]*4;bones=[0]*4
                for i,(g,w) in enumerate(groups):bones[i]=BONE_IDS[o.vertex_groups[g].name];weights[i]=w
                total=sum(weights)
                if not total:raise ValueError('Unweighted garment vertex '+o.name)
                item={'position':xyz(p),'normal':xyz(normal.normalized()),'uv':{'x':uv.data[li].uv.x,'y':uv.data[li].uv.y},'bones':bones,'weights':[w/total for w in weights]}
                local.append(len(entries));entries.append(item)
            material_indices.setdefault(poly.material_index,[]).extend(local)
        for index,indices in material_indices.items():
            mat=o.data.materials[index]
            result['parts'].append({'name':o.name,'material':mat.name,'color':mat.get('color_file',''),'useNativeNormal':bool(o.get('native_uv',False)),'baseColor':list(mat.diffuse_color)[:3],
                'roughness':float(mat.get('roughness',.6)),'metallic':float(mat.get('metallic',0)), 'vertices':entries,'triangles':indices})
    path=DEST/(style+'.json');path.write_text(json.dumps(result,separators=(',',':')),encoding='utf-8')
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS:o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(DEST/(style+'.glb')),use_selection=True,export_format='GLB',export_materials='EXPORT')
    return result

def render(style):
    # Pose the native body and weighted clothing through the same exported bones.
    arm_data=bpy.data.armatures.new('Native anatomy');arm=bpy.data.objects.new('Native anatomy',arm_data);bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for bone in DATA['skeleton']:
        b=arm_data.edit_bones.new(bone['name']);b.head=v3(bone['position']);b.tail=b.head+Vector((0,0,.04))
    bpy.ops.object.mode_set(mode='OBJECT');arm.select_set(False)
    for o in [BODY]+PARTS:
        mod=o.modifiers.new('Native weighted pose','ARMATURE');mod.object=arm
    # Lower the A-pose arms naturally around the actual shoulder pivot.
    for side,angle in [('L',-.52),('R',.52)]:
        # Bone hierarchy is intentionally flat, so rotate all descendants about
        # the shoulder with one world delta, preserving the imported skin bind.
        pivot=BONES['Bip01 '+side+' UpperArm'];rotation=__import__('mathutils').Matrix.Rotation(angle,4,'Y')
        transform=__import__('mathutils').Matrix.Translation(pivot)@rotation@__import__('mathutils').Matrix.Translation(-pivot)
        for b in arm.pose.bones:
            if b.name.startswith('Bip01 '+side+' ') and any(s in b.name for s in ['UpperArm','Forearm','Hand','Finger']):b.matrix=transform@b.matrix
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8 if '--quick' in sys.argv else 24;scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_x=1100;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
    scene.world.color=(.20,.24,.28)
    bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.data.materials.append(material('Studio floor',(.035,.045,.055)))
    for position,power,size in [((2,-3,3.5),220,3),((-2,-2,2),110,2),((0,2,3),280,2.5)]:
        bpy.ops.object.light_add(type='AREA',location=position);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size
        light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=(2.3,-6,2.5));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=2.25
    cam.rotation_euler=(Vector((0,0,.98))-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
    scene.view_settings.view_transform='AgX'
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST/(style+'.blend')))
    scene.render.filepath=str(DEST/(style+'-full.png'));bpy.ops.render.render(write_still=True)
    cam.location=(.8,-4,1.83);cam.data.ortho_scale=.76;cam.rotation_euler=(Vector((0,0,1.51))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(DEST/(style+'-portrait.png'));bpy.ops.render.render(write_still=True)

def create(character,style):
    global DATA,POINTS,FACES,BVH,BONES,BONE_IDS,BODY,PARTS,COVERAGE,DEST
    reset();DEST=OUT/style;DEST.mkdir(exist_ok=True)
    DATA=json.loads((OUT/'Reference'/f'{character}.json').read_text(encoding='utf-8'))
    POINTS=[v3(p) for p in DATA['vertices']];FACES=[];source_mats=[];indices=[]
    BONES={b['name']:v3(b['position']) for b in DATA['skeleton']};BONE_IDS={b['name']:i for i,b in enumerate(DATA['skeleton'])}
    PARTS=[];COVERAGE=[]
    for sub,s in enumerate(DATA['surfaces']):
        for i in range(0,len(s['triangles']),3):FACES.append(tuple(s['triangles'][i:i+3]));indices.append((sub,i//3))
    BVH=BVHTree.FromPolygons(POINTS,FACES,all_triangles=True)
    native=bpy.data.meshes.new(character+' native body');native.from_pydata(POINTS,[],FACES);native.update()
    BODY=bpy.data.objects.new(character+' native facial body',native);bpy.context.collection.objects.link(BODY)
    native_uv=native.uv_layers.new()
    for poly,tri in zip(native.polygons,FACES):
        poly.use_smooth=True
        for li,vi in zip(poly.loop_indices,tri):native_uv.data[li].uv=(DATA['uv'][vi]['x'],DATA['uv'][vi]['y'])
    for s in DATA['surfaces']:
        m=material(s['name'],(.5,.5,.5));image=bpy.data.images.load(str(ROOT/s['color']),check_existing=True)
        node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;shader=m.node_tree.nodes.get('Principled BSDF')
        m.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
        if 'opacity' in s['name']:
            m.node_tree.links.new(node.outputs['Alpha'],shader.inputs['Alpha']);m.surface_render_method='DITHERED'
        native.materials.append(m)
    for poly,(sub,_) in zip(native.polygons,indices):poly.material_index=sub
    for b in DATA['skeleton']:BODY.vertex_groups.new(name=b['name'])
    for i,w in enumerate(DATA['weights']):
        for bone,weight in zip(w['bones'],w['values']):
            if weight>0:BODY.vertex_groups[bone].add([i],weight,'REPLACE')
    hip=BONES['Bip01 Pelvis'].z;neck=BONES['Bip01 Neck'].z
    hero=style=='OpeningHero';mystic=style=='MuseumMystic';teacher=style=='Professor'
    source=ROOT/next(s['color'] for s in DATA['surfaces'] if '_body' in s['name'])
    color=(.025,.075,.035) if hero else (.018,.025,.032) if mystic else (.13,.07,.035)
    cloth=tinted(style+' tailored cloth',color,None if hero else source)
    trim=tinted(style+' woven edging',(.42,.055,.035) if hero else (.025,.30,.32) if mystic else (.38,.25,.13))
    plain=tinted(style+' matching woven cloth',color)
    pants=tinted(style+' fitted trousers',(.65,.18,.025) if hero else (.025,.03,.035),source)
    leather=tinted(style+' leather',(.025,.023,.02) if hero or mystic else (.12,.055,.025),source,rough=.40)
    gold=tinted(style+' aged brass',(.45,.27,.075),rough=.35,metal=.72)
    ivory=tinted(style+' ivory shirt',(.73,.72,.65))
    selected={'torso':[],'pants':[],'boots':[]}
    # Native garment UV islands are separate components. Keep complete shirt,
    # sleeve and trouser components rather than cutting jagged height bands.
    adjacency={}
    for tri,(sub,ordinal) in zip(FACES,indices):
        if '_body' not in DATA['surfaces'][sub]['name']:continue
        for i in tri:adjacency.setdefault(i,set()).update(tri)
    unseen=set(adjacency);regions={}
    while unseen:
        stack=[unseen.pop()];group=set(stack)
        while stack:
            for neighbor in adjacency[stack.pop()]&unseen:
                unseen.remove(neighbor);group.add(neighbor);stack.append(neighbor)
        lo=min(POINTS[i].z for i in group);hi=max(POINTS[i].z for i in group)
        hand=sum(sum(w for b,w in zip(DATA['weights'][i]['bones'],DATA['weights'][i]['values']) if 'Hand' in DATA['skeleton'][b]['name'] or 'Finger' in DATA['skeleton'][b]['name']) for i in group)/len(group)
        kind=None
        if hand<.20 or (not hero and hi>1.1):
            if hi>1.1 and lo>.70:kind='torso'
            elif hi<.20:kind='boots'
            elif hi<1.05:kind='pants'
        for i in group:regions[i]=kind
    for fi,(tri,(sub,ordinal)) in enumerate(zip(FACES,indices)):
        if '_body' not in DATA['surfaces'][sub]['name']:continue
        kind=regions.get(tri[0])
        if kind:
            selected[kind].append((fi,tri));COVERAGE.append({'submesh':sub,'triangle':ordinal})
    visible_torso=selected['torso']
    if hero:visible_torso=[(fi,tri) for fi,tri in visible_torso if sum(POINTS[i].z for i in tri)/3>hip+.025]
    if mystic:visible_torso=[(fi,tri) for fi,tri in visible_torso if sum(POINTS[i].z for i in tri)/3<neck-.025]
    surface('Anatomically fitted jacket',visible_torso,cloth,.009)
    trousers=surface('Continuous tailored trousers',selected['pants'],pants,.009)
    surface('Sculpted boots and soles' if hero else 'Fitted leather shoes',selected['boots'],leather,.008)
    if hero:
        # Split the same continuous weighted surface at the boot cuff, so
        # trousers and boots cannot expose gaps or intersect one another.
        trousers.data.materials.append(leather)
        bm=bmesh.new();bm.from_mesh(trousers.data)
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,.36),plane_no=(0,0,1),clear_outer=False,clear_inner=False)
        for f in bm.faces:f.material_index=1 if f.calc_center_median().z<.36 else 0
        bm.to_mesh(trousers.data);bm.free()
    # Hide precisely covered source polygons; keep hands, face, eyelashes and hair.
    hair_covered=[]
    for fi,(tri,(sub,ordinal)) in enumerate(zip(FACES,indices)):
        z=sum(POINTS[i].z for i in tri)/3
        kai_scalp=hero and '_head' in DATA['surfaces'][sub]['name'] and z>BONES['Bip01 REye'].z+.06
        hide=kai_scalp or (hero and 'opacity' in DATA['surfaces'][sub]['name'] and max(POINTS[i].z for i in tri)>BONES['Bip01 REye'].z+.05) or (mystic and '_body' not in DATA['surfaces'][sub]['name'] and z>BONES['Bip01 REye'].z+.09)
        if hide:COVERAGE.append({'submesh':sub,'triangle':ordinal});hair_covered.append(fi)
    bm=bmesh.new();bm.from_mesh(native);bm.faces.ensure_lookup_table();covered=set(fi for group in selected.values() for fi,_ in group)|set(hair_covered)
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in covered],context='FACES');bm.to_mesh(native);bm.free()
    width=.180 if hero else .197;depth=.145 if hero else .155
    if not teacher:
        shell('Split tailored coat tails',[(hip-.23,width+.045,depth+.035),(hip-.12,width+.03,depth+.020),(hip+.04,width+.01,depth),(hip+.12,width,depth)],plain,split=.035 if hero else .065,pleats=.0025)
        shell('Woven waist sash' if hero else 'Waistcoat belt',[(hip+.09,width+.014,depth+.014),(hip+.13,width+.012,depth+.012)],ivory if hero else trim)
    # Front edges, frog fasteners or tailored lapels follow the actual chest surface.
    if hero:
        for s in [-1,1]:ribbon('Oxblood tunic piping',[(s*.11,neck-.08),(s*.08,neck-.22),(s*.075,hip+.13)],.009,trim)
        for z in [hip+.19,hip+.26,hip+.33]:
            curve('Frog closure',[front(-.03,z,.023),front(0,z+.006,.024),front(.03,z,.023)],.0035,trim)
            ellipsoid('Closure knot',front(.035,z,.027),(.006,.004,.006),gold)
        scarf=tinted('Kai saffron scarf',(.70,.26,.025))
        shell('Soft folded scarf',[(neck-.025,.09,.082),(neck+.025,.082,.075)],scarf,offset=(0,0,0),pleats=.005)
        tail=ribbon('Saffron scarf folded tail',[(.12,neck-.025),(.14,neck-.15),(.15,neck-.31),(.14,hip+.23)],.060,scarf)
        tail.vertex_groups.clear();g=tail.vertex_groups.new(name='Bip01 Spine2');g.add(list(range(len(tail.data.vertices))),1,'REPLACE')
        head='Bip01 Head';top=max(p.z for p in POINTS)
        hair=tinted('Kai swept charcoal hair',(.023,.018,.014),rough=.6)
        hair.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.12
        scalp=[(fi,tri) for fi,(tri,(sub,_)) in enumerate(zip(FACES,indices)) if '_head' in DATA['surfaces'][sub]['name'] and sum(POINTS[i].z for i in tri)/3>BONES['Bip01 REye'].z+.06]
        surface('Sculpted native scalp hair',scalp,hair,.004,smooth=False)
        # Curved locks blend into the preserved native hair instead of a cone crown.
        for i in range(9):
            t=i*math.tau/9;cx=.065*math.cos(t);cy=.06*math.sin(t)
            points=[(cx,cy,top-.055),(cx*.95+.015,cy,top-.005),(cx*.7+.035,cy*.65,top+.025+(i%3)*.012)]
            curve('Swept hair lock',points,.010-(i%2)*.001,hair,head)
        scar=material('Kai healed cheek scar',(.30,.13,.095))
        z=BONES['Bip01 REye'].z-.045
        curve('Subtle healed cheek scar',[front(.058,z+.012,.002),front(.062,z,.0025),front(.057,z-.012,.002)],.0012,scar,head)
    else:
        # White shirt inset with tapered cloth lapels, not rectangular bars.
        points=[front(-.037,neck-.025,.022),front(.037,neck-.025,.022),front(.022,neck-.15,.022),front(0,neck-.23,.022),front(-.022,neck-.15,.022)]
        mesh=bpy.data.meshes.new('Tapered shirt inset');mesh.from_pydata(points,[],[(4,3,2,1,0)]);mesh.update();shirt=bpy.data.objects.new('Tapered shirt inset',mesh);bpy.context.collection.objects.link(shirt);finish(shirt,shirt.name,ivory)
        if mystic:
            for s in [-1,1]:ribbon('Sculpted jacket lapel',[(s*.055,neck-.015),(s*.11,neck-.14),(s*.045,neck-.29)],.025,trim)
        for z in [hip+.18,hip+.25,hip+.32]:ellipsoid('Brass jacket button',front(.026,z,.028),(.006,.004,.006),gold)
        if mystic:
            shell('Draped shoulder mantle',[(neck-.25,.305,.19),(neck-.13,.29,.18),(neck-.015,.19,.115),(neck+.015,.09,.083)],plain)
            shell('Teal mantle hem',[(neck-.25,.308,.193),(neck-.235,.307,.192)],trim)
            for o in PARTS[-2:]:
                o.vertex_groups.clear();g=o.vertex_groups.new(name='Bip01 Spine2');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
            top=BONES['Bip01 REye'].z+.125;head='Bip01 Head'
            shell('Curved top hat brim',[(top-.035,.145,.13),(top-.027,.153,.137),(top-.018,.143,.13)],leather)
            # All hat vertices are rigidly head-weighted for stable gaze/gestures.
            shell('Shaped top hat crown',[(top-.024,.10,.095),(top+.055,.094,.088),(top+.16,.11,.10)],plain)
            shell('Grosgrain teal hat band',[(top-.015,.102,.097),(top+.025,.099,.094)],trim)
            ellipsoid('Hat crown top',(0,0,top+.16),(.11,.10,.012),plain,head)
            for o in PARTS[-4:]:
                o.vertex_groups.clear();g=o.vertex_groups.new(name=head);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
        # Slim spectacles fitted to the actual eye landmarks, preserving pupils.
        head='Bip01 Head'
        eyes=[BONES['Bip01 LEye'],BONES['Bip01 REye']]
        frame=tinted(style+' spectacle bronze',(.14,.10,.065),rough=.28,metal=.65)
        for eye in eyes:
            points=[(eye.x+.023*math.cos(t*math.tau/32),-eye.y-.028,eye.z+.016*math.sin(t*math.tau/32)) for t in range(33)]
            # BONES coordinates already use Blender Y; glasses project toward -Y.
            points=[(p[0],eye.y-.028,p[2]) for p in points]
            curve('Fine spectacle rim',points,.0018,frame,head)
        z=(eyes[0].z+eyes[1].z)/2;y=(eyes[0].y+eyes[1].y)/2-.030
        curve('Spectacle bridge',[(-.007,y,z), (0,y-.008,z+.004),(.007,y,z)],.0015,frame,head)
    result=export(style)
    render(style)
    receipt={'style':style,'character':character,'triangles':sum(len(p['triangles'])//3 for p in result['parts']),
        'coveredSourceTriangles':len(COVERAGE),'bones':len(BONE_IDS),'facialSource':'Original 175-shape source preserved',
        'license':'Original garments; MIT Rocketbox-derived surface anatomy','sha256':hashlib.sha256((DEST/(style+'.json')).read_bytes()).hexdigest()}
    (DEST/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print('TAILORED_READY',json.dumps(receipt),flush=True)

selection=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['OpeningHero','MuseumMystic','Professor']
for name,style in [('Kai','OpeningHero'),('Orion','MuseumMystic'),('Theo','Professor')]:
    if style in selection:create(name,style)

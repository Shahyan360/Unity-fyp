"""Original shaped skeletal studies and arena practice weapon, authored in local Blender.
Interpretive educational sculpture, not a scan or a validated scientific reconstruction.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parents[3]/'Art/Exhibits'
OUT.mkdir(parents=True,exist_ok=True)

def mat(name,color,rough=.8,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m

def mesh(name,vertices,faces,material):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);data.materials.append(material)
    for p in data.polygons:p.use_smooth=True
    uv=data.uv_layers.new(name='Surface metres')
    for p in data.polygons:
        for i in p.loop_indices:
            v=data.vertices[data.loops[i].vertex_index].co;uv.data[i].uv=(v.x,v.z)
    return o

def tube(name,points,radii,material,sides=12):
    points=[Vector(p) for p in points];verts=[];faces=[]
    if len(points)>2:
        # Smooth anatomical curves through the authored control points.
        expanded=[];sizes=[]
        for i in range(len(points)-1):
            p0=points[max(0,i-1)];p1=points[i];p2=points[i+1];p3=points[min(len(points)-1,i+2)]
            for step in range(5):
                t=step/5
                expanded.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
                sizes.append(radii[i]*(1-t)+radii[i+1]*t)
        points=expanded+[points[-1]];radii=sizes+[radii[-1]]
    for j,p in enumerate(points):
        tangent=(points[min(j+1,len(points)-1)]-points[max(j-1,0)]).normalized()
        first=tangent.cross(Vector((0,0,1)))
        if first.length<.01:first=tangent.cross(Vector((0,1,0)))
        first.normalize();second=tangent.cross(first).normalized();r=radii[j]
        for k in range(sides):
            t=k*math.tau/sides;verts.append(p+first*math.cos(t)*r+second*math.sin(t)*r*.86)
    for j in range(len(points)-1):
        for k in range(sides):
            a=j*sides+k;b=j*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+k for k in range(sides))])
    return mesh(name,verts,faces,material)

def bone(name,a,b,width,material):
    a=Vector(a);b=Vector(b);delta=b-a
    fractions=(0,.07,.17,.33,.53,.73,.87,.96,1)
    radius=(.64,1.05,.80,.52,.48,.56,.82,1.07,.64)
    bend=delta.cross(Vector((0,1,0))).normalized()*width*.25
    points=[a+delta*t+bend*math.sin(t*math.pi) for t in fractions]
    return tube(name,points,[width*r for r in radius],material,14)

def plate(name,points,thickness,material):
    # Solid, slightly curved flat bone with a real edge instead of a cuboid.
    points=[Vector(p) for p in points];n=len(points)
    vertices=[p+Vector((0,-thickness/2,0)) for p in points]+[p+Vector((0,thickness/2,0)) for p in points]
    faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh(name,vertices,faces,material)
    bevel=o.modifiers.new('Rounded cortical edge','BEVEL');bevel.width=thickness*.35;bevel.segments=2
    return o

def skull(name,center,size,material):
    horned=size[0]<.5
    sections=[(-.42,.22,.24,-.19),(-.20,.29,.24,-.20),(.05,.27,.17,-.16),(.25,.19,.10,-.12),(.44,.07,.01,-.09)] if horned else [(-.50,.13,.15,-.10),(-.33,.23,.30,-.17),(-.12,.24,.28,-.19),(.10,.20,.15,-.16),(.36,.17,.10,-.14),(.60,.12,.065,-.10),(.67,.04,.03,-.07)]
    vertices=[];faces=[];n=32
    for x,width,top,bottom in sections:
        for i in range(n):
            a=i*math.tau/n;vertices.append((x,width*math.cos(a),(top+bottom)/2+math.sin(a)*(top-bottom)/2))
    for j in range(len(sections)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces += [tuple(reversed(range(n))),tuple((len(sections)-1)*n+i for i in range(n))]
    o=mesh(name,vertices,faces,material);o.location=center
    inner=o.copy();inner.data=o.data.copy();bpy.context.collection.objects.link(inner);inner.scale=(.88,.76,.66)
    bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('Hollow cortical cranium','BOOLEAN');mod.operation='DIFFERENCE';mod.object=inner
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(inner,do_unlink=True)
    openings=[(-.14,.11,.105,.105),(.22,.01,.14,.08),(.48,-.005,.065,.04)] if not horned else [(-.13,.08,.10,.09),(.26,-.01,.065,.04)]
    for x,z,rx,rz in openings:
        pos=(center[0]+x,center[1],center[2]+z);scale=(rx,.48,rz)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=pos);cut=bpy.context.object;cut.scale=scale
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Anatomical fenestra','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut
        bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
    return o

def cranial_frill(height,material):
    contour=[];n=32
    for i in range(n):
        t=i*math.tau/n
        contour.append((1.03-.075*math.sin(t),.57*math.cos(t),height+.53+.40*math.sin(t)))
    vertices=[(p[0]-.026,p[1],p[2]) for p in contour]+[(p[0]+.026,p[1],p[2]) for p in contour]
    faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh('Broad scalloped cranial frill',vertices,faces,material)
    # Paired windows make the exhibit read as bone, not a solid decorative shield.
    for side in (-1,1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=(1.03,side*.25,height+.59));cut=bpy.context.object;cut.scale=(.30,.15,.20)
        bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Frill fenestra','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut
        bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
    return o

def skeleton(horned=False):
    boneMat=mat('Warm fossil ivory',(.62,.54,.40),.88);support=mat('Museum charcoal support',(.055,.065,.064),.65,.5)
    plinth=mat('Honed limestone exhibit base',(.25,.27,.24),.85)
    height=1.55 if horned else 2.04
    # Articulated vertebral bodies and neural processes, graded into the tail.
    spine=[(-2.9,.0,height-.45),(-2.4,0,height-.29),(-1.9,0,height-.12),(-1.4,0,height),(-.85,0,height+.06),(-.3,0,height+.10),(.25,0,height+.12),(.72,0,height+.17),(1.10,0,height+.29)]
    for j in range(len(spine)-1):
        a=Vector(spine[j]);b=Vector(spine[j+1]);length=(b-a).length
        for k in range(3):
            p=a+(b-a)*k/3;w=.025+.025*j/len(spine)
            tube('Individually shaped vertebra',[p,p+(b-a)*.23],[w*1.3,w],boneMat)
            bone('Neural spine',p,p+Vector((-.015,0,.07+j*.012)),w*.45,boneMat)
            if j>3:
                for side in (-1,1):bone('Transverse vertebral process',p,p+Vector((-.025,side*.12,-.025)),.018,boneMat)
    # Open, sweeping rib cage. Each rib tapers organically towards the sternum.
    for j in range(10):
        x=-.8+j*.135;spread=.43*math.sin((j+2)*math.pi/14)
        for side in (-1,1):
            pts=[(x,0,height+.08),(x-.045,side*spread*.7,height-.02),(x-.06,side*spread,height-.28),(x+.02,side*spread*.75,height-.54),(x+.08,side*.09,height-.60)]
            tube('Curved thoracic rib',pts,[.025,.025,.021,.016,.008],boneMat)
    # Scapulae and open pelvic arches have recognizable flat-bone silhouettes.
    for side in (-1,1):
        y=side*.25
        plate('Shaped scapula',[(.50,y,height-.04),(.14,y,height+.02),(-.18,y,height-.12),(.25,y,height-.28)],.045,boneMat)
        tube('Open pelvic girdle',[(-.88,side*.18,height+.02),(-1.13,side*.35,height-.15),(-.93,side*.42,height-.40),(-.65,side*.33,height-.31),(-.68,side*.18,height-.02)],[.07,.065,.055,.05,.065],boneMat)
        hip=(-.9,side*.38,height-.29);knee=(-.47,side*.47,.82 if not horned else .74);ankle=(-.88,side*.47,.25)
        bone('Femur with shaped condyles',hip,knee,.095,boneMat);bone('Tibia and ankle',knee,ankle,.065,boneMat)
        bone('Slender fibula',(-.54,side*.51,knee[2]),(-.91,side*.51,.31),.027,boneMat)
        if horned:
            shoulder=(.52,side*.33,height-.24);elbow=(.69,side*.40,.72);front=(.72,side*.43,.22)
            bone('Humerus',shoulder,elbow,.075,boneMat);bone('Radius',elbow,front,.049,boneMat)
        else:
            shoulder=(.55,side*.30,height-.18);elbow=(.80,side*.43,height-.50);front=(1.03,side*.43,height-.45)
            bone('Short theropod humerus',shoulder,elbow,.04,boneMat);bone('Paired radius ulna',elbow,front,.028,boneMat)
        for foot in ([ankle,front] if horned else [ankle]):
            for toe in (-1,0,1):
                a=Vector(foot);b=a+Vector((.30, toe*.09,-.07));bone('Articulated metatarsal',a,b,.027,boneMat)
                tube('Tapered toe ungual',[b,b+Vector((.10,toe*.018,-.015)),b+Vector((.16,toe*.025,-.035))],[.027,.020,.001],boneMat)
    head=(1.44,0,height+.34)
    skull('Fenestrated dinosaur cranium',head,(.52,.23,.28) if not horned else (.48,.29,.28),boneMat)
    for side in (-1,1):
        tube('Curved lower jaw',[(1.06,side*.17,height+.16),(1.42,side*.22,height+.03),(1.91,side*.13,height+.11)],[.05,.045,.025],boneMat)
        if not horned:
            for i in range(9):
                x=1.28+i*.066;tube('Individual tapered tooth',[(x,side*.17,height+.18),(x+.01,side*.17,height+.07)],[.018,.001],boneMat,8)
    if horned:
        for side in (-1,1):
            tube('Curved brow horn',[(1.36,side*.19,height+.53),(1.65,side*.21,height+.78),(1.97,side*.22,height+.83)],[.095,.054,.001],boneMat,16)
        tube('Nasal horn',[(1.78,0,height+.49),(1.93,0,height+.67)],[.065,.001],boneMat,14)
        cranial_frill(height,boneMat)
    # Purposeful museum mount: two discreet stanchions and an inset base.
    for x in (-.9,.5):tube('Steel conservation support',[(x,0,.12),(x,0,height-.14)],[.018,.018],support)
    plate('Low chamfered display slab',[(-3.25,0,.04),(2.16,0,.04),(2.25,0,.10),(-3.35,0,.10)],1.45,plinth)

def sword():
    wood=mat('Practice ash grain',(.28,.16,.07),.82);grip=mat('Bound leather grip',(.09,.055,.03),.8)
    # Flattened lenticular blade with tapered shoulders and rounded tip.
    levels=[(0,.026),(.12,.026),(.16,.048),(.48,.040),(.63,.026),(.69,.002)]
    verts=[]
    for z,w in levels:
        verts.extend([(-w,0,z),(0,-.014,z),(w,0,z),(0,.014,z)])
    faces=[]
    for j in range(len(levels)-1):
        for k in range(4):faces.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
    mesh('Shaped wooden practice blade',verts,faces,wood)
    tube('Leather wrapped grip',[(0,0,-.12),(0,0,.025)],[.024,.023],grip,14)
    tube('Curved wooden guard',[(-.09,0,.025),(0,0,.045),(.09,0,.025)],[.014,.017,.014],wood,12)

def export(name,make):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);make()
    for o in list(bpy.context.scene.objects):
        if o.type=='MESH':
            bpy.context.view_layer.objects.active=o
            for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB')
    data={'name':name,'source':'Original local Blender mesh authoring','license':'Original project-authored asset','interpretive':name!='ArenaPracticeSword',
          'vertices':sum(len(o.data.vertices) for o in bpy.context.scene.objects if o.type=='MESH'),
          'sha256':hashlib.sha256((OUT/(name+'.glb')).read_bytes()).hexdigest()}
    (OUT/(name+'-receipt.json')).write_text(json.dumps(data,indent=2));print('EXHIBIT_READY',json.dumps(data),flush=True)

export('TheropodSkeleton',lambda:skeleton(False))
export('HornedDinosaurSkeleton',lambda:skeleton(True))
export('ArenaPracticeSword',sword)

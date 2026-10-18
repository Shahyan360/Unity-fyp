"""Reproducible Blender 4.5 asset authoring. Run with blender -b --python this_file."""
import bpy, math, json, pathlib, shutil
from mathutils import Vector

OUT = pathlib.Path(r'D:\LumenArtifacts\Art')
ROOT = pathlib.Path(__file__).resolve().parents[2]
DEST = ROOT / 'Assets/Art/LumenCollection'
OUT.mkdir(parents=True, exist_ok=True)
DEST.mkdir(parents=True, exist_ok=True)
manifest = {}

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def material(name, color, metal=0, rough=.4):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    return m

def finish(o,name,m,bevel=0):
    o.name=name; o.data.materials.append(m)
    if bevel:
        mod=o.modifiers.new('Manufactured edge radius','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in o.data.polygons: p.use_smooth=True
    return o

def box(name,loc,scale,m,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,m,bevel)

def cylinder(name,loc,r,depth,m,rot=(0,0,0),vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc,rotation=rot)
    return finish(bpy.context.object,name,m,.008)

def sphere(name,loc,r,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=r,location=loc)
    return finish(bpy.context.object,name,m)

def torus(name,loc,r,tube,m,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=10,location=loc,major_radius=r,minor_radius=tube,rotation=rot)
    return finish(bpy.context.object,name,m)

def rod(name,a,b,r,m):
    a,b=Vector(a),Vector(b); o=cylinder(name,(a+b)/2,r,(b-a).length,m)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); return o

def lettering(text,loc,size,m,rot=(math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rot); o=bpy.context.object; o.name='Engraving '+text
    o.data.body=text; o.data.size=size; o.data.align_x='CENTER'; o.data.extrude=.001
    o.data.materials.append(m); bpy.ops.object.convert(target='MESH')

def render_and_export(name,look_at=(0,0,1),camera=(3,-4,2.8)):
    objects=list(bpy.context.scene.objects)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects if o.type=='MESH')
    mats={m.name:{'color':list(m.diffuse_color),'metallic':m.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value,
                  'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value}
          for o in objects if o.type=='MESH' for m in o.data.materials}
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True)
    manifest[name]={'triangles':tris,'materials':mats,'source':'Original procedural geometry authored for Lumen using Blender Python','license':'Project-owned original work'}
    # A neutral studio is preview-only and never included in the exported geometry.
    floor=material('Preview floor',(.025,.035,.045),0,.6)
    box('Preview floor',(0,0,-.065),(200,200,.1),floor,0)
    scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=32
    scene.cycles.use_denoising=True; scene.render.resolution_x=1100; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
    scene.world.color=(.22,.22,.22)
    for pos,energy,size in [((3,-4,6),1100,5),((-4,-1,3),800,4),((1,4,5),1500,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=energy; light.data.shape='DISK'; light.data.size=size
        light.rotation_euler=(Vector(look_at)-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=camera); cam=bpy.context.object; cam.rotation_euler=(Vector(look_at)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO'; cam.data.ortho_scale=3.5; scene.camera=cam
    scene.render.filepath=str(OUT/(name+'.png'))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
    bpy.ops.render.render(write_still=True)
    print('LUMEN_ASSET',name,tris,flush=True)

def espresso():
    reset(); cream=material('Porcelain enamel',(.78,.71,.55),.2,.3); chrome=material('Brushed steel',(.48,.55,.59),.9,.22)
    dark=material('Midnight graphite',(.025,.043,.049),.35,.34); wood=material('Walnut',(.15,.067,.033),0,.45)
    brass=material('Satin brass',(.63,.36,.10),.8,.28); coffee=material('Espresso',(.035,.012,.005),0,.2)
    # Freestanding café service cabinet, appliance and cups.
    box('Walnut cabinet',(0,0,.45),(1.6,.72,.85),wood)
    for x in [-.74,.74]: box('Cabinet leg',(x,0,.07),(.07,.60,.14),dark,.01)
    box('Stone counter',(0,0,.91),(1.72,.82,.08),cream)
    for x in [-.39,.39]:
        box('Recessed cabinet door',(x,-.366,.48),(.73,.025,.67),wood,.009)
        rod('Cabinet handle',(x-.13,-.40,.68),(x+.13,-.40,.68),.012,brass)
    box('Espresso chassis',(0,.10,1.26),(1.12,.48,.58),cream,.075)
    box('Front fascia',(0,-.155,1.32),(1.02,.045,.32),chrome,.018)
    box('Drip tray',(0,-.25,1.005),(1.12,.33,.06),chrome,.015)
    for i in range(23): box('Drip tray slot',(-.5+i*.045,-.25,1.04),(.014,.23,.006),dark,.003)
    for x in [-.27,.27]:
        cylinder('Brew group',(x,-.19,1.20),.085,.10,chrome,(math.pi/2,0,0))
        rod('Portafilter handle',(x,-.21,1.19),(x,-.46,1.19),.028,dark)
        cylinder('Control bezel',(x,-.19,1.42),.033,.02,brass,(math.pi/2,0,0))
        for i in [-1,1]: cylinder('Button',(x+i*.07,-.19,1.42),.012,.02,dark,(math.pi/2,0,0))
        cylinder('Porcelain cup',(x,-.30,1.095),.05,.10,cream)
        cylinder('Coffee surface',(x,-.30,1.147),.041,.004,coffee)
        torus('Cup handle',(x+.057,-.30,1.10),.027,.008,cream,(math.pi/2,0,0))
    cylinder('Pressure gauge',(0,-.192,1.42),.055,.026,dark,(math.pi/2,0,0))
    cylinder('Gauge face',(0,-.208,1.42),.046,.005,cream,(math.pi/2,0,0))
    rod('Gauge needle',(0,-.213,1.42),(.021,-.213,1.448),.003,dark)
    rod('Steam pipe',(.48,-.18,1.3),(.48,-.28,1.17),.012,chrome)
    rod('Steam wand',(.48,-.28,1.17),(.53,-.28,1.06),.012,chrome)
    for z in [1.59,1.65]: rod('Cup rail',(-.48,-.1,z),(.48,-.1,z),.008,brass)
    lettering('L U M E N',(0,-.182,1.53),.045,dark)
    render_and_export('EspressoStation',(0,0,.85),(3,-4,2.9))

def orrery():
    reset(); dark=material('Orrery midnight',(.018,.052,.072),.45,.32); brass=material('Orrery brass',(.62,.38,.12),.85,.23)
    cream=material('Orrery lettering',(.81,.74,.53),.45,.35)
    cylinder('Display foot',(0,0,.06),.63,.12,dark)
    cylinder('Pedestal',(0,0,.44),.25,.74,dark)
    for z in [.12,.80,.86]: cylinder('Brass collar',(0,0,z),.28 if z<.85 else .73,.025,brass)
    cylinder('Precision base',(0,0,.85),.74,.11,dark)
    for i in range(72):
        a=i*math.tau/72; o=box('Engraved degree mark',(.68*math.cos(a),.68*math.sin(a),.911),(.025 if i%6 else .05,.005,.002),cream,0); o.rotation_euler.z=a
    rod('Central shaft',(0,0,.88),(0,0,1.55),.018,brass)
    sun=material('Sun amber',(.94,.48,.055),.45,.22); sphere('Sun',(0,0,1.55),.14,sun)
    colors=[(.35,.31,.26),(.72,.48,.17),(.06,.34,.52),(.65,.17,.08),(.65,.49,.30),(.76,.65,.40),(.22,.64,.67),(.08,.24,.54)]
    for i,c in enumerate(colors):
        r=.22+i*.105; a=i*2.27; z=1.55
        torus('Orbital teaching track',(0,0,z),r,.004,brass)
        x,y=r*math.cos(a),r*math.sin(a)
        rod('Planet arm',(0,0,1.0+i*.045),(x,y,1.0+i*.045),.009,brass)
        rod('Planet pin',(x,y,1.0+i*.045),(x,y,z),.006,brass)
        sphere(['Mercury','Venus','Earth','Mars','Jupiter','Saturn','Uranus','Neptune'][i],(x,y,z),[.025,.043,.047,.034,.09,.08,.063,.06][i],material('Planet '+str(i),c,.1,.38))
        if i==5: torus('Saturn rings',(x,y,z),.12,.012,cream,(.3,.15,0))
    lettering('O R R E R Y',(0,-.747,.84),.055,cream)
    render_and_export('DiscoveryOrrery',(0,0,.9),(3,-4,3.2))

def roman():
    reset(); stone=material('Warm travertine',(.64,.53,.37),0,.75); pale=material('Limestone edges',(.81,.73,.57),0,.65)
    dark=material('Roman plinth charcoal',(.035,.048,.05),.18,.45); brass=material('Roman plaque brass',(.58,.34,.10),.75,.32)
    box('Exhibit plinth',(0,0,.28),(1.85,.85,.56),dark,.04); box('Exhibit slab',(0,0,.60),(1.94,.94,.09),pale,.015)
    for x in [-.65,.65]:
        box('Column square base',(x,0,.72),(.38,.40,.16),stone,.015)
        for z,r,d in [(.81,.19,.05),(.87,.16,.07),(1.58,.16,.06),(1.64,.20,.06)]: cylinder('Column moulding',(x,0,z),r,d,pale)
        # Fluted column shaft is a continuous mesh, not overlapping primitive strips.
        verts=[]; faces=[]; n=120
        for z,r in [(.90,.13),(1.54,.112)]:
            for i in range(n):
                a=i*math.tau/n; flute=1-.07*(.5+.5*math.cos(a*20)); verts.append((x+r*flute*math.cos(a),r*flute*math.sin(a),z))
        for i in range(n): faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
        mesh=bpy.data.meshes.new('Fluted shaft'); mesh.from_pydata(verts,[],faces); mesh.update(); ob=bpy.data.objects.new('Fluted shaft',mesh); bpy.context.collection.objects.link(ob); finish(ob,'Fluted shaft',stone)
        box('Capital',(x,0,1.70),(.40,.42,.09),pale,.012)
    box('Entablature',(0,0,1.81),(1.85,.44,.15),stone,.015)
    box('Cornice',(0,0,1.91),(1.99,.52,.07),pale,.01)
    for x in [-.8,-.6,-.4,-.2,0,.2,.4,.6,.8]: box('Dentil',(x,-.25,1.86),(.07,.08,.08),pale,.005)
    mesh=bpy.data.meshes.new('Pediment'); mesh.from_pydata([(-1,-.25,1.96),(1,-.25,1.96),(0,-.25,2.29),(-1,.25,1.96),(1,.25,1.96),(0,.25,2.29)],[],[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]); mesh.update()
    ob=bpy.data.objects.new('Pediment',mesh); bpy.context.collection.objects.link(ob); finish(ob,'Pediment',stone)
    box('Plaque',(0,-.438,.39),(.95,.025,.20),brass,.009)
    lettering('ROMAN ARCHITECTURE',(0,-.456,.40),.055,dark)
    lettering('INTERPRETIVE STUDY',(0,-.456,.33),.035,dark)
    render_and_export('RomanStudy',(0,0,1.05),(3,-4,3))

espresso(); orrery(); roman()
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))
# Import is deliberately a separate step after the previews have been inspected.
print('LUMEN_PREVIEWS_READY',str(OUT),flush=True)

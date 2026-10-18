"""Original compact museum collection. Blender 4.5; all working files on D.

Run blender -b --python Tools/Art/create_compact_museum.py
No external geometry or textures. Exports are staged on D before Unity admission.
"""
import bpy, math, json, random, sys
from pathlib import Path
from mathutils import Vector

OUT = Path('D:/LumenProject/Art')
OUT.mkdir(parents=True, exist_ok=True)
random.seed(24)
receipt = {}

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def mat(name, color, metal=0, rough=.65):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    return m

def finish(o,name,m,bevel=0):
    o.name=name; o.data.materials.append(m)
    if bevel:
        mod=o.modifiers.new('Soft crafted edges','BEVEL'); mod.width=bevel; mod.segments=2
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def box(name,pos,size,m,bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,m,bevel)

def cyl(name,pos,r,depth,m,vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=pos)
    return finish(bpy.context.object,name,m,.008)

def ellipsoid(name,pos,scale,m,segments=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=8,radius=1,location=pos)
    o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons: p.use_smooth=True
    return finish(o,name,m)

def rod(name,a,b,r,m):
    a,b=Vector(a),Vector(b); o=cyl(name,(a+b)/2,r,(b-a).length,m,12)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); return o

def curve(name,points,r,m):
    c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.bevel_depth=r; c.bevel_resolution=2
    s=c.splines.new('POLY'); s.points.add(len(points)-1)
    for p,v in zip(s.points,points): p.co=(*v,1)
    o=bpy.data.objects.new(name,c); bpy.context.collection.objects.link(o); o.data.materials.append(m)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.convert(target='MESH'); o.select_set(False)

def text(body,pos,size,m):
    bpy.ops.object.text_add(location=pos,rotation=(math.pi/2,0,math.pi))
    o=bpy.context.object; o.data.body=body; o.data.align_x='CENTER'; o.data.size=size
    o.data.extrude=.003; o.data.materials.append(m); bpy.ops.object.convert(target='MESH')

def export(name,target,campos,ortho):
    bpy.ops.object.select_all(action='DESELECT')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    for o in meshes: o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),use_selection=True,export_format='GLB',export_materials='EXPORT')
    receipt[name]={'triangles':tris,'mesh_objects':len(meshes),'source':'Original Blender Python geometry','license':'Project original; no third-party geometry'}
    scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24
    scene.render.resolution_x=1200; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
    scene.world.color=(.2,.2,.2)
    for pos,power,size in [(campos,1800,8),((-8,-3,12),1400,8),((0,10,14),2200,10)]:
        bpy.ops.object.light_add(type='AREA',location=pos); l=bpy.context.object
        l.data.energy=power; l.data.shape='DISK'; l.data.size=size
        l.rotation_euler=(Vector(target)-l.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=campos); c=bpy.context.object
    c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler()
    c.data.type='ORTHO'; c.data.ortho_scale=ortho; scene.camera=c
    scene.render.filepath=str(OUT/(name+'.png'))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
    bpy.ops.render.render(write_still=True)
    print('COMPACT_ASSET',name,tris,flush=True)

def courtyard():
    reset()
    stone=mat('Warm limestone',(.63,.55,.42)); trim=mat('Carved ivory stone',(.82,.75,.60))
    green=mat('Weathered copper',(.12,.29,.24),.45); red=mat('Oxblood woven banner',(.25,.032,.035))
    brass=mat('Patinated bronze',(.42,.26,.08),.65,.4); leaves=mat('Trimmed boxwood',(.075,.20,.065))
    foliage=mat('Cypress foliage',(.055,.12,.047)); water=mat('Still reflecting pool',(.045,.19,.19),.4,.16)
    dark=mat('Wet charcoal basin',(.11,.13,.12)); flower=mat('Ivory garden flowers',(.88,.84,.65))
    box('Courtyard foundation',(0,5.5,-.14),(15,11,.28),stone,.06)
    # A level route leads directly through the open doorway; no decorative stair barrier.
    for row in range(11):
        for col in range(3):
            box('Limestone paving joint',((col-1)*.94,.5+row,.015),(.91,.96,.045),trim,.035)
    for side in [-1,1]:
        x=side*4.65
        box('Facade masonry wing',(side*6.8,0,2.9),(7.4,.65,5.8),stone,.06)
        for z in [.2,5.35,5.75]: box('Continuous cornice',(side*6.8,.05,z),(7.5,.95,.16),trim)
        box('Oxblood welcome banner',(side*6.0,.4,3.05),(1.35,.055,3.5),red,.012)
        for z in [1.32,4.8]: rod('Bronze banner bar',(side*6.0-.78,.48,z),(side*6.0+.78,.48,z),.04,brass)
        text('LUMEN',(side*6.0,.46,3.1),.20,trim)
        for cx in [side*3.15,side*4.30]:
            box('Column square foot',(cx,.68,.14),(.78,.9,.28),trim)
            cyl('Column plinth',(cx,.68,.38),.35,.2,trim)
            cyl('Fluted column shaft',(cx,.68,2.8),.27,4.65,trim,40)
            for k in range(12):
                a=k*math.tau/12
                rod('Column fluting',(cx+.263*math.cos(a),.68+.263*math.sin(a),.55),(cx+.263*math.cos(a),.68+.263*math.sin(a),5.05),.026,stone)
            cyl('Doric capital',(cx,.68,5.16),.39,.21,trim)
            box('Column abacus',(cx,.68,5.35),(.82,.88,.18),trim)
        box('Raised pool coping',(x,5,.18),(3.65,4.8,.36),trim,.10)
        box('Shallow basin',(x,5,.38),(3.25,4.4,.07),dark,.05)
        box('Calm water',(x,5,.425),(3.08,4.22,.025),water,.04)
        for y in [2.25,7.75]:
            box('Garden planter',(x,y,.25),(3.7,.7,.5),stone,.06)
            box('Clipped hedge',(x,y,.62),(3.5,.6,.46),leaves,.18)
            for j in range(7):
                fx=x-1.45+j*.48
                rod('Flower stalk',(fx,y,.8),(fx,y,1.1),.012,leaves)
                ellipsoid('White bloom',(fx,y,1.13),(.075,.075,.11),flower,12)
        # Original garden urns avoid relying on the unnormalised portrait scan.
        cyl('Garden statue base',(x,5,.53),.72,.2,stone)
        cyl('Garden statue pedestal',(x,5,1.0),.40,.75,trim)
        cyl('Garden statue capital',(x,5,1.43),.52,.12,trim)
        profile=[(.20,1.50),(.23,1.57),(.14,1.65),(.17,1.73),(.30,1.82),(.39,2.04),(.32,2.27),(.22,2.40),(.26,2.47),(.28,2.50)]
        verts=[]; faces=[]; n=40
        for r,z in profile:
            verts.extend([(x+r*math.cos(k*math.tau/n),5+r*math.sin(k*math.tau/n),z) for k in range(n)])
        for j in range(len(profile)-1):
            for k in range(n):
                a=j*n+k; b=j*n+(k+1)%n
                faces.append((a,b,b+n,a+n))
        mesh=bpy.data.meshes.new('Garden urn profile'); mesh.from_pydata(verts,[],faces); mesh.update()
        urn=bpy.data.objects.new('Classical garden urn',mesh); bpy.context.collection.objects.link(urn); finish(urn,'Classical garden urn',trim)
        for p in mesh.polygons:p.use_smooth=True
        for s in [-1,1]:
            curve('Carved urn handle',[(x+s*(.26+.18*math.sin(k*math.pi/16)),5,2.36-k*.030) for k in range(17)],.045,trim)
        cyl('Urn dark inner opening',(x,5,2.43),.22,.015,dark)
        for y in [1.3,9.4]:
            for xx in [side*6.55]:
                cyl('Cypress planter',(xx,y,.32),.54,.64,stone)
                rod('Cypress trunk',(xx,y,.45),(xx,y,2.3),.07,dark)
                for z,r in [(1.4,.52),(2.0,.48),(2.6,.36),(3.1,.22)]:
                    ellipsoid('Cypress canopy',(xx,y,z),(r,r,.72),foliage)
        for y in [2,8.7]:
            lx=side*1.92
            cyl('Lantern base',(lx,y,.10),.20,.20,brass)
            rod('Lantern post',(lx,y,.2),(lx,y,1.4),.045,brass)
            box('Lantern amber core',(lx,y,1.53),(.20,.20,.29),flower)
            for dx in [-.13,.13]:
                for dy in [-.13,.13]: rod('Lantern mullion',(lx+dx,y+dy,1.35),(lx+dx,y+dy,1.72),.018,brass)
            box('Lantern cap',(lx,y,1.75),(.38,.38,.08),brass)
    box('Portico entablature',(0,.4,5.66),(10,.95,.48),trim)
    for x in range(-23,24): box('Cornice dentil',(x*.21,.94,5.53),(.10,.20,.15),stone,.005)
    # Triangular pediment, extruded toward the courtyard.
    verts=[(-5,-.25,5.9),(5,-.25,5.9),(0,-.25,7.55),(-5,1.05,5.9),(5,1.05,5.9),(0,1.05,7.55)]
    mesh=bpy.data.meshes.new('Pediment'); mesh.from_pydata(verts,[],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]); mesh.update()
    o=bpy.data.objects.new('Classical pediment',mesh); bpy.context.collection.objects.link(o); finish(o,'Classical pediment',stone,.03)
    for a,b in [((-5.1,1.1,5.98),(0,1.1,7.67)),((0,1.1,7.67),(5.1,1.1,5.98))]: rod('Pediment molding',a,b,.10,trim)
    for side in [-1,1]:
        o=box('Copper roof plane',(side*2.5,.2,6.79),(5.35,1.8,.10),green); o.rotation_euler.y=side*math.atan(1.65/5)
    badge=cyl('Museum bronze medallion',(0,1.13,6.48),.40,.07,brass,48); badge.rotation_euler.x=math.pi/2
    text('MUSEUM',(0,.96,5.62),.24,brass)
    export('MuseumCourtyard',(0,4,2),(15,22,15),23)

def fossils():
    reset()
    wood=mat('Fossil cabinet walnut',(.12,.065,.033)); bronze=mat('Fossil brass',(.56,.35,.12),.65,.4)
    rock=mat('Sedimentary shale',(.24,.22,.18)); bone=mat('Ammonite fossil ochre',(.67,.46,.24))
    dark=mat('Fossil display backing',(.038,.05,.046)); ivory=mat('Fossil identification ivory',(.83,.75,.60))
    box('Cabinet body',(0,0,.48),(2.65,.88,.90),wood,.045)
    for x in [-1.12,1.12]:
        for y in [-.30,.30]: box('Cabinet foot',(x,y,.09),(.12,.12,.18),bronze)
    box('Display bronze lip',(0,0,.96),(2.75,.98,.08),bronze)
    box('Recessed felt tray',(0,0,1.01),(2.58,.82,.04),dark)
    slab=box('Shale ammonite slab',(-.55,.03,1.07),(1.12,.67,.10),rock,.08)
    # Schematic spiral and radial chambers. Interpretive reconstruction, not a scan.
    points=[]
    for i in range(180):
        a=i/179*math.tau*2.3; r=.025+.022*a
        points.append((-.55+r*math.cos(a),.02+r*math.sin(a),1.145))
    curve('Ammonite coiled shell',points,.034,bone)
    for i in range(12,180,6):
        a=i/179*math.tau*2.3; r=.025+.022*a
        rod('Shell chamber rib',(-.55+(r-.035)*math.cos(a),.02+(r-.035)*math.sin(a),1.17),(-.55+(r+.038)*math.cos(a+.04),.02+(r+.038)*math.sin(a+.04),1.17),.012,ivory)
    box('Leaf impression shale',(.70,.03,1.07),(.86,.67,.10),rock,.06)
    rod('Fern impression stem',(.40,-.20,1.13),(.94,.24,1.13),.012,bone)
    for i in range(9):
        t=i/9; x=.42+t*.48; y=-.17+t*.38
        for side in [-1,1]: rod('Fern leaflet impression',(x,y,1.14),(x-side*.08,y+side*.12,1.14),.018,bone)
    box('Cabinet label plate',(0,.473,.65),(2.15,.018,.20),bronze)
    text('F O S S I L   S T U D I E S',(0,.49,.60),.105,ivory)
    export('FossilCabinet',(0,0,.65),(3,4,3.2),3.9)

def roman_case():
    reset()
    wood=mat('Roman case walnut',(.12,.065,.033)); brass=mat('Roman case bronze',(.50,.31,.10),.65,.4)
    clay=mat('Roman terracotta pottery',(.44,.20,.095)); dark=mat('Roman case felt',(.035,.045,.04))
    cream=mat('Roman case lettering',(.82,.73,.55)); gold=mat('Replica Roman coin',(.58,.41,.16),.72,.36)
    box('Artifact cabinet',(0,0,.48),(2.35,.82,.90),wood,.035)
    box('Bronze display edge',(0,0,.96),(2.43,.90,.08),brass)
    box('Recessed artifact tray',(0,0,1.01),(2.28,.77,.04),dark)
    profile=[(.12,1.04),(.14,1.10),(.10,1.16),(.20,1.27),(.27,1.45),(.22,1.65),(.11,1.78),(.09,1.96),(.13,2.0)]
    verts=[]; faces=[]; n=32
    for r,z in profile: verts.extend([(-.55+r*math.cos(k*math.tau/n),r*math.sin(k*math.tau/n),z) for k in range(n)])
    for j in range(len(profile)-1):
        for k in range(n):
            a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
    mesh=bpy.data.meshes.new('Amphora profile');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Interpretive amphora',mesh);bpy.context.collection.objects.link(o);finish(o,'Interpretive amphora',clay)
    for p in mesh.polygons:p.use_smooth=True
    for side in [-1,1]:
        curve('Amphora loop handle',[(-.55+side*(.1+.24*math.sin(k*math.pi/20)),0,1.91-k*.023) for k in range(21)],.025,clay)
    for x,y,r in [(.26,.03,.20),(.77,.08,.15)]:
        box('Coin display stand',(x,y,1.11),(.16,.18,.16),brass)
        c=cyl('Replica coin',(x,y,1.31),r,.025,gold,48);c.rotation_euler.x=math.pi/2
        pts=[(x+r*.86*math.cos(k*math.tau/60),y+.019,1.31+r*.86*math.sin(k*math.tau/60)) for k in range(61)]
        curve('Coin relief rim',pts,.008,cream)
        ellipsoid('Schematic coin portrait',(x,y+.024,1.33),(r*.33,.012,r*.48),gold)
    box('Roman case label',(0,.43,.62),(2.0,.018,.20),brass)
    text('T R A D E  /  D A I L Y  L I F E',(0,.445,.58),.075,cream)
    export('RomanArtifactCase',(0,0,1),(3,4,3.1),3.8)

def forum():
    reset()
    wood=mat('Forum dark walnut',(.12,.061,.028)); bronze=mat('Forum bronze edging',(.48,.30,.095),.65)
    stone=mat('Forum limestone',(.72,.63,.47)); roof=mat('Forum terracotta',(.39,.16,.078)); paving=mat('Forum paving',(.40,.36,.29))
    green=mat('Forum miniature cypress',(.08,.17,.06))
    box('Museum tabletop',(0,0,.94),(3.9,2.9,.16),wood,.06)
    box('Table bronze edge',(0,0,1.03),(3.94,2.94,.025),bronze,.025)
    box('Miniature stone site',(0,0,1.075),(3.75,2.75,.065),paving)
    for x in [-1.6,1.6]:
        for y in [-1.1,1.1]: box('Carved table leg',(x,y,.46),(.17,.17,.92),wood)
    # One compact, interpretive forum; deliberately not a geographical reconstruction.
    def temple(x,y,w=.72,d=1.1):
        for i in range(3): box('Temple podium',(x,y,1.13+i*.045),(w+.20-i*.05,d+.20-i*.05,.05),stone,.008)
        box('Temple cella',(x,y+.10,1.45),(w*.45,d*.58,.50),stone,.008)
        for side in [-1,1]:
            for j in range(5):
                cx=x+side*w*.43; cy=y-d*.40+j*d*.20
                cyl('Miniature colonnade',(cx,cy,1.46),.032,.52,stone,12)
                cyl('Miniature capital',(cx,cy,1.73),.044,.04,stone,12)
        for j in [-1,0,1]: cyl('Temple front column',(x+j*w*.28,y-d*.43,1.46),.032,.52,stone,12)
        box('Temple entablature',(x,y,1.79),(w+.08,d+.08,.09),stone,.008)
        for side in [-1,1]:
            o=box('Terracotta temple roof',(x+side*w*.25,y,1.91),(w*.58,d+.16,.035),roof,.004); o.rotation_euler.y=side*.40
        for j in range(9): rod('Ridge tile',(x-.37*w,y-d*.44+j*d*.11,1.88),(x+.37*w,y-d*.44+j*d*.11,1.88),.008,roof)
    temple(-.95,0)
    temple(.0,.65,.65,.85)
    # Open elliptical amphitheatre with three tiers of arch openings, not a solid ring.
    for tier in range(3):
        z=1.15+tier*.17
        for i in range(30):
            a=i*math.tau/30; x=.91+.66*math.cos(a); y=-.29+.48*math.sin(a)
            rod('Amphitheatre pier',(x,y,z),(x,y,z+.13),.021,stone)
            pts=[]
            for j in range(7):
                u=(i+j/6)*math.tau/30
                pts.append((.91+.66*math.cos(u),-.29+.48*math.sin(u),z+.105+.045*math.sin(j/6*math.pi)))
            curve('Amphitheatre arch',pts,.017,stone)
        pts=[(.91+.67*math.cos(i*math.tau/120),-.29+.49*math.sin(i*math.tau/120),z+.17) for i in range(121)]
        curve('Amphitheatre cornice',pts,.025,stone)
    for side in [-1,1]:
        for i in range(5):
            x=-1.45+i*.48; y=side*1.08
            box('Miniature Roman house',(x,y,1.25),(.32,.30,.28),stone,.008)
            for s in [-1,1]:
                o=box('House pitched roof',(x+s*.09,y,1.43),(.20,.36,.025),roof,.004); o.rotation_euler.y=s*.45
    for x,y in [(-1.65,-.6),(-1.65,.6),(.35,-.9),(1.65,.6)]:
        ellipsoid('Miniature cypress',(x,y,1.35),(.055,.055,.25),green,12)
    box('Forum model title plaque',(0,1.49,.91),(2.4,.025,.20),bronze)
    text('ROME  /  AN INTERPRETIVE STUDY',(0,1.51,.875),.09,stone)
    export('ForumTable',(0,0,1),(4.5,5,4.4),5.8)

if __name__ == '__main__':
    selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['courtyard','fossils','forum','roman_case']
    for name in selected: {'courtyard':courtyard,'fossils':fossils,'forum':forum,'roman_case':roman_case}[name]()
    receipt_path=OUT/'compact-collection.json'
    previous=json.loads(receipt_path.read_text()) if receipt_path.exists() else {}
    previous.update(receipt); receipt_path.write_text(json.dumps(previous,indent=2))

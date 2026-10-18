"""Full-sized original Roman market props. Blender 4.5, working output on D."""
import sys, math, json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_compact_museum as a
import bpy

a.reset()
wood=a.mat('Market aged oak',(.22,.115,.047)); edge=a.mat('Market end grain',(.12,.055,.022))
cloth=a.mat('Market oxblood canvas',(.34,.045,.035)); linen=a.mat('Market undyed linen',(.70,.60,.40))
clay=a.mat('Market terracotta',(.48,.20,.08)); dark=a.mat('Market shadow',(.055,.032,.014))
leaf=a.mat('Market olive leaf',(.15,.24,.045)); apple=a.mat('Market red fruit',(.52,.065,.025))
gold=a.mat('Market ochre produce',(.72,.42,.07)); iron=a.mat('Market forged iron',(.055,.06,.055),.6)

# Real dimensions: 3.6 m wide, 2.5 m deep, 2.95 m high. Floor at z=0.
for x in [-1.55,1.55]:
    for y in [-.85,.85]:
        a.box('Market canopy post',(x,y,1.42),(.13,.13,2.84),wood,.018)
        a.box('Iron post foot',(x,y,.13),(.16,.16,.26),iron,.012)
        a.rod('Timber knee brace',(x,y,2.30),(x-(.43 if x>0 else -.43),y,2.78),.045,wood)
for y,z in [(-.9,2.88),(.9,2.55)]: a.box('Market crossbeam',(0,y,z),(3.48,.12,.14),edge)
for x in [-1.6,1.6]: a.rod('Sloping canopy rail',(x,-1.1,2.96),(x,1.1,2.56),.055,wood)
for i in range(12):
    x=-1.8+i*.3
    verts=[]
    for k in range(9):
        y=-1.15+k*.3; z=2.99-.18*(y+1.15)-.09*math.sin(k*math.pi/8)
        verts.extend([(x,y,z),(x+.3,y,z)])
    faces=[(k*2,k*2+1,k*2+3,k*2+2) for k in range(8)]
    mesh=bpy.data.meshes.new('Canvas strip');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Striped cloth canopy',mesh);bpy.context.collection.objects.link(o)
    a.finish(o,'Striped cloth canopy',cloth if i%2==0 else linen)
    # Double-sided cloth edge reads correctly from inside and from the street.
    mod=o.modifiers.new('Canvas thickness','SOLIDIFY');mod.thickness=.008
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    a.box('Hanging canopy valance',(x+.15,1.26,2.49),(.30,.015,.20),cloth if i%2==0 else linen,.008)

a.box('Market counter',(0,.38,.49),(3.12,.82,.92),wood,.025)
for i in range(14):
    x=-1.48+i*.225
    a.box('Counter vertical oak board',(x,.80,.48),(.205,.04,.86),wood,.012)
    for z in [.15,.81]:
        pin=a.cyl('Counter nail',(x,.827,z),.012,.015,iron,8);pin.rotation_euler.x=math.pi/2
a.box('Counter top',(0,.38,.98),(3.28,.96,.10),edge)
for x in [-.95,0,.95]:
    a.box('Produce bin base',(x,.42,1.055),(.80,.68,.045),wood)
    for y in [.06,.78]:a.box('Produce bin rail',(x,y,1.16),(.82,.035,.20),linen)
    for xx in [x-.40,x+.40]:a.box('Produce bin side',(xx,.42,1.16),(.035,.72,.20),wood)
    for k in range(12):
        px=x-.26+(k%4)*.17;py=.2+(k//4)*.18
        a.ellipsoid('Fresh market produce',(px,py,1.17),(.085,.08,.07),apple if x<0 else gold if x==0 else leaf,12)
for x,y in [(-1.2,-.4),(1.2,-.4)]:
    a.box('Storage crate',(x,y,.28),(.65,.55,.56),wood)
    for z in [.08,.26,.44]:a.box('Crate slat',(x,y+.29,z),(.66,.035,.12),linen,.008)
    a.rod('Crate diagonal reinforcement',(x-.28,y+.32,.05),(x+.28,y+.32,.51),.025,edge)
# Two clay storage jars, with a proper turned profile and handles.
for x,y in [(-2.0,.35),(2.0,.35)]:
    profile=[(.19,.02),(.23,.08),(.28,.2),(.34,.48),(.27,.70),(.13,.85),(.13,.98),(.17,1.02)]
    verts=[];faces=[];n=24
    for r,z in profile:verts.extend([(x+r*math.cos(k*math.tau/n),y+r*math.sin(k*math.tau/n),z) for k in range(n)])
    for j in range(len(profile)-1):
        for k in range(n):
            v=j*n+k;w=j*n+(k+1)%n;faces.append((v,w,w+n,v+n))
    mesh=bpy.data.meshes.new('Storage amphora');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Clay market amphora',mesh);bpy.context.collection.objects.link(o);a.finish(o,'Clay market amphora',clay)
    for p in mesh.polygons:p.use_smooth=True
    for s in [-1,1]:a.curve('Amphora handle',[(x+s*(.14+.20*math.sin(k*math.pi/16)),y,.94-k*.025) for k in range(17)],.033,clay)
    a.cyl('Jar inner shadow',(x,y,.96),.125,.02,dark)
a.export('RomanMarketStall',(0,0,1.25),(5.5,7,4.5),6.2)
(a.OUT/'roman-city-collection.json').write_text(json.dumps(a.receipt,indent=2))

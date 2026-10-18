"""Full-scale original Roman temple restoration, not a tabletop exhibit."""
import sys,math,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_compact_museum as a
import bpy
a.reset()
stone=a.mat('Temple pale limestone',(.65,.61,.50));trim=a.mat('Temple carved ivory',(.78,.74,.63));roof=a.mat('Temple terracotta roof',(.38,.14,.065));wood=a.mat('Temple oak doors',(.12,.065,.025));bronze=a.mat('Temple bronze medallion',(.42,.27,.10),.65)
for i in range(3):a.box('Temple stepped podium',(0,0,.15+i*.30),(8.8-i*.28,7.2-i*.28,.30),stone,.035)
for i in range(6):a.box('Approach stair',(0,3.40+(5-i)*.29,.075+i*.15),(3.2,1.9-(i*.29),.15),stone,.015)
for x in [-2.80,2.80]:a.box('Cella side wall',(x,-.4,2.45),(.25,4.9,3.1),stone)
a.box('Cella rear wall',(0,-2.8,2.45),(5.85,.25,3.1),stone)
for x in [-1.95,1.95]:a.box('Cella doorway wing',(x,1.95,2.45),(1.7,.28,3.1),stone)
a.box('Cella doorway lintel',(0,1.95,3.75),(2.2,.40,.5),trim)
for x in [-1.12,1.12]:a.box('Open timber door',(x,1.72,2.10),(.10,.50,2.40),wood)
for x in [-3.2,-1.92,-.64,.64,1.92,3.2]:
    a.box('Column square plinth',(x,2.95,.99),(.75,.75,.18),trim)
    a.cyl('Column base moulding',(x,2.95,1.13),.34,.12,trim,40)
    verts=[];faces=[];n=96
    for z,r in [(1.19,.27),(3.97,.235)]:
        for j in range(n):
            angle=j*math.tau/n;radius=r*(1-.06*(.5+.5*math.cos(angle*16)));verts.append((x+radius*math.cos(angle),2.95+radius*math.sin(angle),z))
    for j in range(n):faces.append((j,(j+1)%n,(j+1)%n+n,j+n))
    mesh=bpy.data.meshes.new('Temple fluted shaft');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Fluted limestone column',mesh);bpy.context.collection.objects.link(o);a.finish(o,o.name,stone)
    a.cyl('Capital echinus',(x,2.95,4.04),.34,.14,trim,40)
    a.box('Capital abacus',(x,2.95,4.16),(.75,.75,.12),trim)
a.box('Temple architrave',(0,0,4.34),(8.25,7.0,.28),stone)
a.box('Temple projecting cornice',(0,0,4.55),(8.65,7.40,.15),trim)
for x in [i*.34-4.08 for i in range(25)]:a.box('Front cornice dentil',(x,3.66,4.43),(.12,.19,.17),trim,.008)
for side in [-1,1]:
    panel=a.box('Pitched tiled temple roof',(side*2.15,0,5.22),(4.62,7.60,.14),roof);panel.rotation_euler.y=side*.31
    for j in range(31):
        y=-3.65+j*.245;a.rod('Terracotta imbrex ridge',(0,y,5.95),(side*4.42,y,4.53),.045,roof)
for y in [-3.72,3.72]:
    v=[(-4.25,y,4.63),(4.25,y,4.63),(0,y,5.97),(-4.25,y+.12,4.63),(4.25,y+.12,4.63),(0,y+.12,5.97)]
    mesh=bpy.data.meshes.new('Temple triangular pediment');mesh.from_pydata(v,[],[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]);mesh.update();o=bpy.data.objects.new('Real triangular pediment',mesh);bpy.context.collection.objects.link(o);a.finish(o,o.name,stone)
    for x in [-1,1]:a.rod('Pediment raking cornice',(0,y+.10,6.02),(x*4.35,y+.10,4.64),.075,trim)
a.cyl('Pediment bronze shield',(0,3.86,5.16),.30,.055,bronze,40).rotation_euler.x=math.pi/2
a.export('RomanTempleRestoration',(0,0,2.8),(12,15,10),13)
(a.OUT/'roman-temple-receipt.json').write_text(json.dumps(a.receipt,indent=2))

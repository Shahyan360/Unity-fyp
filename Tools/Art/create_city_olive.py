"""Original olive street planter; Blender API, staged and rendered on D."""
import sys,math,random,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_compact_museum as a
import bpy
random.seed(318)
a.reset()
stone=a.mat('Olive planter limestone',(.48,.45,.36));soil=a.mat('Olive planter soil',(.08,.065,.035));bark=a.mat('Olive gnarled bark',(.20,.16,.10))
leaves=[a.mat('Olive silver leaves '+str(i),c) for i,c in enumerate([(.22,.28,.12),(.30,.36,.20),(.16,.23,.09),(.38,.40,.25)])]
a.box('Stone planter',(0,0,.3),(1.25,1.25,.60),stone,.07)
a.box('Dark planting soil',(0,0,.61),(1.08,1.08,.06),soil)
for x in [-.59,.59]:a.box('Planter rim',(x,0,.65),(.13,1.31,.13),stone)
for y in [-.59,.59]:a.box('Planter rim',(0,y,.65),(1.31,.13,.13),stone)
a.curve('Twisting olive trunk',[(0,0,.62),(.06,0,1.1),(-.05,.03,1.6),(.04,.05,2.05)],.075,bark)
for k in range(9):
    angle=k*math.tau/9;end=(math.cos(angle)*.78,math.sin(angle)*.75,2.15+(k%3)*.20)
    a.rod('Olive branch',(0,0,1.4+k*.05),end,.025,bark)
    for j in range(42):
        t=random.random();x=end[0]*t+random.uniform(-.43,.43);y=end[1]*t+random.uniform(-.4,.4);z=end[2]+random.uniform(-.2,.46)
        mesh=bpy.data.meshes.new('Olive leaf mesh')
        mesh.from_pydata([(-.095,0,0),(.095,0,0),(0,-.027,0),(0,.027,0),(0,0,.008),(0,0,-.008)],[],[(0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)])
        mesh.update();leaf=bpy.data.objects.new('Narrow olive leaf',mesh);bpy.context.collection.objects.link(leaf);leaf.location=(x,y,z);a.finish(leaf,leaf.name,random.choice(leaves))
        leaf.rotation_euler=(random.random(),random.random(),random.random()*math.tau)
a.export('CityOlivePlanter',(0,0,1.5),(4,-6,4),4)
(a.OUT/'city-olive-receipt.json').write_text(json.dumps(a.receipt,indent=2))

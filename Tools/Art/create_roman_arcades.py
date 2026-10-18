"""Stone arch retrofit matched to the retained authored amphitheatre dimensions."""
import sys,math,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_compact_museum as a
import bpy
from mathutils import Vector

a.reset()
stones=[a.mat('Arcade limestone '+str(i),c) for i,c in enumerate([(.65,.56,.42),(.72,.64,.49),(.60,.51,.38)])]
for tier in range(3):
    rx=12.8-tier*.42;ry=8.8-tier*.42;cornice=3.02+tier*2.42
    for bay in range(40):
        angle=bay*math.tau/40;following=(bay+1)*math.tau/40
        p=Vector((rx*math.cos(angle),ry*math.sin(angle),0));q=Vector((rx*math.cos(following),ry*math.sin(following),0))
        centre=(p+q)/2;t=(q-p).normalized();normal=Vector((t.y,-t.x,0))
        outer=(q-p).length/2;inner=outer-.19;spring=cornice-outer
        for block in range(9):
            start=block*math.pi/9+.008;end=(block+1)*math.pi/9-.008
            verts=[]
            for depth in [-.18,.18]:
                for r,theta in [(inner,start),(outer,start),(outer,end),(inner,end)]:
                    v=centre+t*(r*math.cos(theta))+normal*depth
                    verts.append((v.x,v.y,spring+r*math.sin(theta)))
            faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
            mesh=bpy.data.meshes.new('Arch voussoir');mesh.from_pydata(verts,[],faces);mesh.update()
            o=bpy.data.objects.new('Tier %d bay %d stone %d'%(tier+1,bay,block),mesh);bpy.context.collection.objects.link(o)
            a.finish(o,o.name,stones[(bay+block+tier)%3])
a.export('RomanArcadeRestoration',(0,0,4.1),(24,32,19),34)
(a.OUT/'roman-arcade-collection.json').write_text(json.dumps(a.receipt,indent=2))

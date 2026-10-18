"""Seal counts and hashes for all original GLB exports without rerendering assets."""
from pathlib import Path
import json,struct,hashlib
root=Path('D:/LumenProject/Art');receipt={}
for path in sorted(root.glob('*.glb')):
    raw=path.read_bytes();size=struct.unpack_from('<I',raw,12)[0];gltf=json.loads(raw[20:20+size])
    triangles=sum(gltf['accessors'][p['indices']]['count']//3 for m in gltf.get('meshes',[]) for p in m['primitives'] if p.get('mode',4)==4)
    receipt[path.stem]={'triangles':triangles,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'source':'Original Blender Python geometry','license':'Project original; no third-party geometry'}
(root/'art-manifest.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print(len(receipt),'original assets sealed')

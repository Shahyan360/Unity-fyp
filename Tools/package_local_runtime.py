"""Copy the existing interpreter and dependency closure. Never installs/downloads.
Private acceptance staging only; final shipped-asset rights review is separate.
"""
from pathlib import Path
import importlib.metadata as metadata
import sys, shutil, json, hashlib
from packaging.requirements import Requirement

base=Path(__file__).resolve().parents[2]
destination=base/'PackageStaging'
destination.mkdir(exist_ok=True)
marker=destination/'LUMEN_TASK_OWNED.txt'
if any(destination.iterdir()) and not marker.exists():raise SystemExit('Refusing unrelated staging directory')
marker.write_text('Local Lumen implementation staging; not public-release clearance.\n')
runtime=destination/'Runtime/Python';runtime.mkdir(parents=True,exist_ok=True)
original=Path(sys.base_prefix)
for item in original.iterdir():
 if item.name not in ('DLLs','Lib','LICENSE.txt','python.exe','pythonw.exe','python3.dll','python312.dll','vcruntime140.dll','vcruntime140_1.dll'):continue
 target=runtime/item.name
 if item.is_dir():shutil.copytree(item,target,dirs_exist_ok=True,ignore=shutil.ignore_patterns('site-packages','__pycache__','test','tests','idlelib','tkinter','ensurepip','*.pyc'))
 else:shutil.copy2(item,target)
site=runtime/'Lib/site-packages';site.mkdir(parents=True,exist_ok=True)
pending=['Flask','Flask-Cors','requests','Pillow','flask-sock','google-genai','faster-whisper','numpy']
seen=set();records=[]
while pending:
 name=pending.pop();dist=metadata.distribution(name);canonical=dist.metadata['Name'].lower().replace('_','-')
 if canonical in seen:continue
 seen.add(canonical);count=0;size=0
 for entry in dist.files or []:
  if '..' in Path(entry).parts or '__pycache__' in Path(entry).parts or str(entry).endswith('.pyc'):continue
  source=Path(dist.locate_file(entry));target=site/entry
  if not source.is_file():continue
  target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target);count+=1;size+=source.stat().st_size
 license_value=dist.metadata.get('License-Expression') or dist.metadata.get('License') or 'Unknown; review included distribution notices'
 records.append({'name':dist.metadata['Name'],'version':dist.version,'files':count,'bytes':size,'licenseMetadata':license_value,'notices':'Runtime/Python/Lib/site-packages/'+str(dist._path.name)})
 print('COPIED',dist.metadata['Name'],dist.version,count,size,flush=True)
 for raw in dist.requires or []:
  dependency=Requirement(raw)
  if dependency.marker is None or dependency.marker.evaluate({'extra':''}):pending.append(dependency.name)
# Preserve only the model files and model card; exclude the download cache.
model=Path('D:/LumenTools/VoiceModels/base.en');modelTarget=destination/'embodied-ai-backend/runtime/models/base.en'
shutil.copytree(model,modelTarget,dirs_exist_ok=True,ignore=shutil.ignore_patterns('.cache','__pycache__'))
voice=Path('D:/LumenTools/Piper');voiceTarget=destination/'Runtime/Piper'
shutil.copytree(voice,voiceTarget,dirs_exist_ok=True,ignore=shutil.ignore_patterns('__pycache__'))
backend=Path(__file__).resolve().parents[1]/'embodied-ai-backend'
shutil.copytree(backend,destination/'embodied-ai-backend',dirs_exist_ok=True,ignore=shutil.ignore_patterns('.venv','__pycache__','.pytest_cache','.env','.env.*','tests','*.pyc','*.log'))
identity=Path(__file__).resolve().parents[1]/'Assets/Resources/LumenCharacterIdentities.json'
shutil.copy2(identity,destination/'embodied-ai-backend/data/characters.json')
(destination/'Tools').mkdir(exist_ok=True);shutil.copy2(Path(__file__).with_name('Start-Lumen.ps1'),destination/'Tools/Start-Lumen.ps1')
(destination/'RUNTIME_INVENTORY.json').write_text(json.dumps({'python':sys.version,'distributions':sorted(records,key=lambda r:r['name']),'scope':'Copied installed runtime for private acceptance. No packages installed. Piper/espeak exact binary-source/redistribution closure and final ledger still require review.'},indent=2),encoding='utf-8')
(destination/'runtime-closure.txt').write_text('\n'.join(r['name']+'=='+r['version'] for r in sorted(records,key=lambda r:r['name']))+'\n')
print('STAGED',destination,len(records),'distributions',flush=True)

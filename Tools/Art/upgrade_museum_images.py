"""Stage public-domain Met originals on D and generate bounded runtime images."""
from pathlib import Path
from PIL import Image
import json,urllib.request,concurrent.futures,hashlib
root=Path('D:/LumenProject/References/MetOpenAccess');out=root/'HighResolution';out.mkdir(parents=True,exist_ok=True)
def prepare(number):
    record=json.loads((root/f'Met{number}.json').read_text(encoding='utf-8'))
    if not record.get('isPublicDomain'):raise ValueError('Missing public-domain verification')
    original=out/f'Met{number}-original.jpg'
    if not original.exists():urllib.request.urlretrieve(record['primaryImage'],original)
    image=Image.open(original).convert('RGB');image.thumbnail((2048,2048));target=out/f'Met{number}.jpg';image.save(target,quality=94)
    return {'object':number,'source':record['primaryImage'],'license':'Public Domain / Met Open Access','size':image.size,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
results=list(concurrent.futures.ThreadPoolExecutor(3).map(prepare,[436535,436105,11417]))
(out/'manifest.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps(results))

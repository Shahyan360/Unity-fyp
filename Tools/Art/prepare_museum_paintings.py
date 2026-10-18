"""Download only public-domain Met images and retain authoritative object records."""
import urllib.request,json,pathlib,shutil,random
from PIL import Image,ImageDraw
root=pathlib.Path(__file__).resolve().parents[2]
out=pathlib.Path('D:/LumenProject/References/MetOpenAccess');out.mkdir(parents=True,exist_ok=True)
dest=root/'Assets/Resources/MuseumPaintings';dest.mkdir(parents=True,exist_ok=True)
records=[]
for number in [436535,436105,11417]:
    try:
        url=f'https://collectionapi.metmuseum.org/public/collection/v1/objects/{number}'
        req=urllib.request.Request(url,headers={'User-Agent':'Lumen educational project asset preparation'})
        data=json.load(urllib.request.urlopen(req,timeout=30))
        if not data.get('isPublicDomain'):raise ValueError('Object is not marked public domain')
        image=data.get('primaryImageSmall') or data.get('primaryImage')
        if not image:raise ValueError('No public image')
        target=out/f'Met{number}.jpg'
        with urllib.request.urlopen(image,timeout=45) as response:target.write_bytes(response.read())
        im=Image.open(target);im.thumbnail((1600,1600));im.convert('RGB').save(dest/target.name,quality=91)
        (out/f'Met{number}.json').write_text(json.dumps(data,indent=2))
        records.append({'id':number,'title':data['title'],'artist':data.get('artistDisplayName'),'date':data.get('objectDate'),'source':data.get('objectURL'),'image':image,'license':'Public Domain / Met Open Access','file':target.name})
    except Exception as error:records.append({'id':number,'error':str(error)})
(out/'manifest.json').write_text(json.dumps(records,indent=2));(dest/'manifest.json').write_text(json.dumps(records,indent=2))
textures=root/'Assets/Resources/EnvironmentTextures';textures.mkdir(parents=True,exist_ok=True)
random.seed(77);im=Image.new('RGB',(1024,1024),(58,57,52));draw=ImageDraw.Draw(im)
for row in range(8):
    for col in range(-1,9):
        x=col*128+(64 if row%2 else 0);y=row*128
        grey=random.randrange(103,138)
        draw.rounded_rectangle((x+3,y+3,x+125,y+125),radius=5,fill=(grey+4,grey+2,grey-8))
pixels=im.load()
for y in range(1024):
    for x in range(1024):
        c=pixels[x,y];n=random.randint(-5,5);pixels[x,y]=tuple(max(0,min(255,v+n)) for v in c)
im.save(textures/'LimestonePavers.png')
print(json.dumps(records,indent=2))

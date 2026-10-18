"""Original seamless honed-limestone floor texture; deterministic, no external imagery."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import random
random.seed(619)
im=Image.new('RGB',(1024,1024),(205,202,187));draw=ImageDraw.Draw(im)
for row in range(4):
    for col in range(4):
        v=random.randrange(203,218);draw.rectangle((col*256+2,row*256+2,(col+1)*256-2,(row+1)*256-2),fill=(v,v-2,v-12))
for i in range(160):
    x=random.randrange(1024);y=random.randrange(1024)
    points=[(x,y)]
    for j in range(random.randrange(3,12)):
        x+=random.randrange(-15,16);y+=random.randrange(5,20);points.append((x,y))
    draw.line(points,fill=(192,190,177),width=1)
im=im.filter(ImageFilter.GaussianBlur(.45))
dest=Path(__file__).resolve().parents[2]/'Assets/Resources/EnvironmentTextures/MuseumLimestone.png'
im.save(dest)

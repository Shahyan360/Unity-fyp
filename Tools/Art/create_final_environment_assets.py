"""Original full-size environment and wardrobe assets; Blender 4.5 API, D-only output."""
import sys,math,json,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import create_compact_museum as a
import bpy,bmesh
random.seed(52)

def shell(name,levels):
    verts=[];faces=[];n=32
    for z,rx,ry in levels:
        verts.extend([(rx*math.cos(i*math.tau/n),ry*math.sin(i*math.tau/n),z) for i in range(n)])
    for j in range(len(levels)-1):
        for i in range(n):
            v=j*n+i;w=j*n+(i+1)%n;faces.append((v,w,w+n,v+n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    for polygon in mesh.polygons:polygon.use_smooth=True
    return o

def block():
    a.reset();plaster=a.mat('Insula warm plaster',(.69,.57,.40));stone=a.mat('Insula limestone',(.49,.47,.40))
    red=a.mat('Pompeian red dado',(.32,.065,.035));wood=a.mat('Shop dark oak',(.15,.075,.027));roof=a.mat('Roman terracotta tiles',(.40,.16,.075));dark=a.mat('Window recess',(.045,.035,.025));leaf=a.mat('Courtyard olive',(.15,.24,.065))
    a.box('Building foundation',(0,0,.16),(7.2,5.6,.32),stone,.04)
    # Two-storey building; real open shop doorway on the street side.
    a.box('Rear masonry wall',(0,-2.55,3.1),(7,.32,5.9),plaster)
    for x in [-3.35,3.35]:a.box('Side masonry wall',(x,0,3.1),(.3,5.4,5.9),plaster)
    for x in [-2.50,2.50]:a.box('Street shop wall',(x,2.55,1.55),(1.7,.35,2.8),plaster)
    a.box('Street upper facade',(0,2.55,4.53),(7,.35,3.0),plaster)
    a.box('Upper storey floor',(0,0,3.0),(6.9,5.3,.18),wood)
    for x in [-2.5,2.5]:a.box('Red street dado',(x,2.75,.65),(1.7,.035,.95),red)
    a.box('Shop lintel',(0,2.65,2.84),(3.65,.55,.27),stone)
    for x in [-1.8,1.8]:a.box('Door stone jamb',(x,2.65,1.53),(.22,.5,2.4),stone)
    for x in [-2.2,0,2.2]:
        a.box('Deep upper window',(x,2.75,4.65),(1.1,.07,1.35),dark)
        for s in [-1,1]:
            o=a.box('Timber window shutter',(x+s*.61,2.86,4.65),(.50,.075,1.40),wood);o.rotation_euler.z=s*.14
            for k in range(6):a.box('Shutter slat',(x+s*.61,2.91,4.12+k*.20),(.47,.045,.055),stone,.004)
        a.box('Window stone sill',(x,2.87,3.94),(1.4,.40,.12),stone)
    for z in [.35,3.1,5.97]:a.box('Facade cornice',(0,2.76,z),(7.3,.38,.16),stone)
    for side in [-1,1]:
        o=a.box('Pitched terracotta roof',(0,side*1.42,6.5),(7.65,3.1,.15),roof);o.rotation_euler.x=-side*.30
        for i in range(26):
            x=-3.65+i*.29
            a.rod('Roof imbrex tile',(x,0,6.97),(x,side*2.95,6.08),.042,roof)
    for x in [-3.25,3.25]:
        a.rod('Shop awning post',(x,3.55,0),(x,3.55,2.50),.055,wood)
    o=a.box('Linen shop shade',(0,3.15,2.64),(6.8,1.6,.055),a.mat('Undyed shop canvas',(.72,.65,.49)));o.rotation_euler.x=-.12
    for x in [-2.0,2.0]:a.box('Shop bench',(x,1.5,.6),(1.35,.65,1.0),wood)
    a.text('TABERNA',(0,2.98,3.30),.30,wood)
    a.export('RomanInsula',(0,0,3.3),(11,15,10),13)

def skeleton(name,quadruped=False):
    a.reset();bone=a.mat('Fossil warm ivory',(.62,.46,.27));edge=a.mat('Fossil darker joints',(.31,.22,.13));base=a.mat('Fossil exhibition slate',(.10,.13,.12));metal=a.mat('Conservation support',(.055,.06,.055),.7)
    height=1.8 if quadruped else 2.7
    a.box('Low fossil display base',(0,0,.12),(9.6,3.6,.24),base,.12)
    for i in range(19):
        x=-1.55+i*.15;z=height+.10*math.sin(i/18*math.pi)
        a.ellipsoid('Spinal vertebra',(x,0,z),(.10,.14,.12),bone,12)
        a.rod('Neural spine',(x,0,z),(x,0,z+.22),.026,bone)
        if i%2==0:
            r=.40+.32*math.sin(i/18*math.pi)
            for side in [-1,1]:
                a.curve('Curved rib',[(x,0,z),(x+.08,side*r*.65,z-.12),(x+.16,side*r,z-.48),(x+.18,side*r*.65,z-.90)],.035,bone)
    for i in range(22):
        x=-1.7-i*.14;z=height-.015*i-.001*i*i
        a.ellipsoid('Tail vertebra',(x,.07*math.sin(i*.13),z),(.085,.10-i*.003,.09-i*.002),bone,12)
        if i<12:a.rod('Tail process',(x,0,z),(x,.18-i*.01,z-.10),.018,bone)
    for side in [-1,1]:
        hip=(-1.0,side*.55,height-.2);knee=(-.5,side*.70,height*.53);ankle=(-1.15,side*.70,.55)
        a.ellipsoid('Pelvic girdle',hip,(.33,.14,.38),bone)
        a.rod('Femur',hip,knee,.10,bone);a.ellipsoid('Knee joint',knee,(.13,.12,.13),bone)
        a.rod('Tibia',knee,ankle,.075,bone);a.rod('Metatarsal',ankle,(-.75,side*.7,.30),.06,bone)
        for toe in [-1,0,1]:a.rod('Articulated foot',(-.75,side*.7,.30),(-.25,side*.7+toe*.16,.27),.035,bone)
        shoulder=(.95,side*.4,height-.2)
        elbow=(1.25,side*.60,height-.6);wrist=(1.55,side*.5,.45 if quadruped else height-.8)
        a.rod('Forelimb humerus',shoulder,elbow,.055,bone);a.rod('Forelimb radius',elbow,wrist,.035,bone)
        for digit in [-1,1]:a.rod('Forelimb digit',wrist,(wrist[0]+.22,wrist[1]+digit*.08,wrist[2]-.13),.02,bone)
        a.rod('Dark specimen support',(-1,side*.70,.26),(-1,side*.70,height-.2),.018,metal)
    for i in range(5):a.ellipsoid('Neck vertebra',(1.25+i*.15,0,height+i*.08),(.12,.15,.13),bone,12)
    hz=height+.4
    for side in [-1,1]:
        # Open skull fenestrae and jaw, rather than a solid cartoon head.
        for p,q in [((1.7,side*.38,hz+.25),(2.7,side*.26,hz+.05)),((2.7,side*.26,hz+.05),(2.9,side*.24,hz-.15)),((1.7,side*.38,hz+.25),(1.6,side*.40,hz-.3)),((1.6,side*.40,hz-.3),(2.9,side*.24,hz-.40)),((2.9,side*.24,hz-.40),(2.9,side*.24,hz-.15)),((1.75,side*.38,hz+.1),(2.4,side*.32,hz-.28))]:a.rod('Skull bone strut',p,q,.065,bone)
        bpy.ops.mesh.primitive_torus_add(major_radius=.19,minor_radius=.045,major_segments=24,minor_segments=8,location=(1.85,side*.39,hz+.08),rotation=(math.pi/2,0,0));a.finish(bpy.context.object,'Orbit bone ring',bone)
        for i in range(9):
            x=1.9+i*.11
            bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=.008,radius2=.035,depth=.13,location=(x,side*.29,hz-.2));a.finish(bpy.context.object,'Fossil tooth',bone)
        if quadruped:
            a.rod('Brow horn',(1.85,side*.27,hz+.20),(2.60,side*.32,hz+.75),.07,bone)
            a.curve('Frill rim',[(1.55,side*.12,hz+.1),(1.38,side*.55,hz+.50),(1.15,side*.55,hz+.95),(1.12,0,hz+1.05)],.07,bone)
    if quadruped:a.rod('Nasal horn',(2.55,0,hz),(2.7,0,hz+.4),.07,bone)
    a.export(name,(0,0,1.6),(10,13,7),12)

def wardrobe(kind):
    a.reset();ivory=a.mat(kind+' linen',(.79,.73,.56));red=a.mat(kind+' oxblood',(.35,.035,.035));green=a.mat(kind+' green',(.055,.17,.09));gold=a.mat(kind+' gold',(.84,.49,.055),.2)
    black=a.mat(kind+' charcoal',(.025,.033,.035));teal=a.mat(kind+' teal',(.035,.39,.42),.3);brown=a.mat(kind+' coat',(.22,.11,.055));metal=a.mat(kind+' bronze',(.53,.35,.12),.7);hair=a.mat(kind+' hair',(.018,.014,.012))
    roman=kind in ['RomanCitizen','RomanGuard'];mystic=kind=='MuseumMystic';teacher=kind=='Professor';hero=kind=='OpeningHero'
    fabric=ivory if roman else black if mystic else brown if teacher else green
    body=shell('Chest_Tailored garment',[(.95,.21,.14),(1.15,.20,.14),(1.39,.245,.145),(1.47,.21,.12),(1.50,.11,.09)])
    a.finish(body,body.name,fabric)
    skirt=shell('Hips_Draped hem',[(.62 if roman else .72,.31 if mystic or teacher else .27,.21 if roman else .20 if mystic or teacher else .17),(.92,.22,.17 if roman else .15),(1.01,.21,.145)])
    if mystic or teacher:
        bm=bmesh.new();bm.from_mesh(skirt.data)
        bmesh.ops.delete(bm,geom=[face for face in bm.faces if face.calc_center_median().y<0 and abs(face.calc_center_median().x)<.075],context='FACES')
        bm.to_mesh(skirt.data);bm.free()
    a.finish(skirt,skirt.name,fabric)
    belt=shell('Hips_Woven belt',[(.97 if hero or roman else .99,.235,.175),(1.03,.235,.175)]);a.finish(belt,belt.name,metal if roman else ivory if hero else teal if mystic else black)
    if roman:
        skin=a.mat(kind+' exposed calf',(.58,.40,.28))
        for x in [-.23,.23]:
            sleeve=shell('LeftUpperArm_Linen sleeve' if x>0 else 'RightUpperArm_Linen sleeve',[(1.28,.083,.095),(1.43,.09,.105),(1.50,.085,.095)])
            sleeve.location.x=x;a.finish(sleeve,sleeve.name,ivory)
        sash=a.box('Chest_Red toga sash',(.02,-.16,1.28),(.13,.028,.53),red,.018);sash.rotation_euler.y=-.50
        for x in [-.11,.11]:
            # Continuous leg anatomy under the short tunic, including the thigh.
            a.ellipsoid('LeftUpperLeg_Thigh' if x>0 else 'RightUpperLeg_Thigh',(x,0,.73),(.082,.088,.26),skin)
            a.ellipsoid('LeftLowerLeg_Calf' if x>0 else 'RightLowerLeg_Calf',(x,0,.29),(.075,.08,.23),skin)
            a.ellipsoid('LeftFoot_Foot' if x>0 else 'RightFoot_Foot',(x,-.035,.095),(.065,.10,.045),skin)
            a.box('LeftFoot_Sandal' if x>0 else 'RightFoot_Sandal',(x,-.035,.055),(.13,.25,.07),brown)
        if kind=='RomanGuard':
            a.finish(shell('Chest_Bronze cuirass',[(1.05,.22,.15),(1.35,.255,.16),(1.44,.21,.13)]),'Chest_Bronze cuirass',metal)
            a.ellipsoid('Head_Helmet',(0,0,1.72),(.115,.115,.09),metal)
            a.box('Head_Red helmet crest',(0,.0,1.85),(.045,.23,.17),red,.025)
    else:
        for x in [-.10,.10]:
            if hero:
                a.ellipsoid('LeftUpperLeg_Trousers' if x>0 else 'RightUpperLeg_Trousers',(x,0,.70),(.115,.115,.28),gold)
                a.ellipsoid('LeftLowerLeg_Boot' if x>0 else 'RightLowerLeg_Boot',(x,.0,.27),(.075,.082,.23),black,16)
            else:
                upper=shell('LeftUpperLeg_Trousers' if x>0 else 'RightUpperLeg_Trousers',[(.46,.075,.085),(.72,.09,.10),(.99,.10,.11)]);upper.location.x=x;a.finish(upper,upper.name,black)
                lower=shell('LeftLowerLeg_Trouser hem' if x>0 else 'RightLowerLeg_Trouser hem',[(.08,.065,.085),(.3,.07,.085),(.55,.075,.085)]);lower.location.x=x;a.finish(lower,lower.name,black)
            a.ellipsoid('LeftFoot_Boot toe' if x>0 else 'RightFoot_Boot toe',(x,-.055,.055),(.075,.135,.055),brown if teacher else black,16)
        if mystic:
            a.cyl('Head_Top hat brim',(0,0,1.77),.18,.025,black,48)
            a.cyl('Head_Top hat crown',(0,0,1.91),.115,.26,black,48)
            a.cyl('Head_Teal hat band',(0,0,1.81),.119,.055,teal,48)
            a.finish(shell('Chest_Mystic shoulder cape',[(1.27,.31,.18),(1.45,.25,.15),(1.50,.12,.10)]),'Chest_Mystic shoulder cape',black)
            for x in [-.15,.15]:a.rod('Chest_Teal lapel',(x,-.17,1.46),(0,-.18,1.15),.018,teal)
        if teacher:
            for x in [-.08,.08]:
                lapel=a.box('Chest_Coat lapel',(x,-.16,1.29),(.055,.018,.33),ivory,.008);lapel.rotation_euler.y=-.25 if x>0 else .25
            for i in range(5):a.ellipsoid('Chest_Coat button',(.06,-.16,1.1+i*.06),(.012,.008,.012),metal,12)
        if teacher or mystic:
            for x in [-.045,.045]:
                bpy.ops.mesh.primitive_torus_add(major_radius=.032,minor_radius=.004,major_segments=24,minor_segments=6,location=(x,-.107,1.68),rotation=(math.pi/2,0,0));a.finish(bpy.context.object,'Head_Spectacle frame',black)
            a.rod('Head_Glasses bridge',(-.013,-.11,1.68),(.013,-.11,1.68),.004,black)
        if hero:
            for i in range(9):
                angle=i*math.tau/9;pos=(.075*math.cos(angle),.07*math.sin(angle),1.735+.015*(i%3))
                if hero:
                    pos=(pos[0],pos[1],1.80+.015*(i%3))
                    bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=.045,radius2=0,depth=.13,location=pos)
                    tuft=a.finish(bpy.context.object,'Head_Swept hair',hair);tuft.rotation_euler.y=math.cos(angle)*.65;tuft.rotation_euler.x=math.sin(angle)*.65
                else:
                    tuft=a.ellipsoid('Head_Swept hair',pos,(.06,.055,.07),brown,12);tuft.rotation_euler.y=(i-4)*.16
        if hero:
            a.finish(shell('Chest_Golden scarf',[(1.46,.14,.12),(1.52,.13,.11)]),'Chest_Golden scarf',gold)
            a.box('Chest_Scarf tail',(.13,-.19,1.29),(.10,.025,.42),gold,.015)
            a.curve('Head_Cheek scar',[(.065,-.108,1.60),(.08,-.108,1.585),(.073,-.108,1.57)],.003,red)
            for i in range(4):a.rod('Chest_Red frog closure',(-.04,-.15,1.13+i*.075),(.04,-.15,1.13+i*.075),.009,red)
    # Preserve named body attachments in wardrobe GLBs (do not combine these prefabs).
    a.export(kind+'Outfit',(0,0,1.1),(2.5,-4,2.4),2.7)

selection=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['block','skeleton','wardrobes']
if 'block' in selection:block()
if 'skeleton' in selection:skeleton('TheropodSkeleton');skeleton('HornedDinosaurSkeleton',True)
if 'wardrobes' in selection:
    for kind in ['OpeningHero','MuseumMystic','Professor','RomanCitizen','RomanGuard']:
        if len(selection)>1 and kind not in selection[1:]:continue
        wardrobe(kind)
(a.OUT/'final-environment-collection.json').write_text(json.dumps(a.receipt,indent=2))

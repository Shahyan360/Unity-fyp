"""Locally authored, weighted wardrobe pass; reads preserved Unity anatomy exports.
Writes only this implementation's Art directory. No provider or network calls.
"""
from pathlib import Path
import sys, math, json, hashlib
HERE=Path(__file__).resolve()
PROJECT=HERE.parents[2]
IMPLEMENTATION=PROJECT.parent
source=(HERE.parent/'create_tailored_characters.py').read_text()
source=source[:source.index('selection=sys.argv')]
source=source.replace("OUT=Path('D:/LumenProject/Art/CharacterTailoring')", "OUT=Path("+repr(str(IMPLEMENTATION/'Art/CharacterTailoring'))+")")
# Ease the curator mantle over the shoulders and use interpolated rig weights.
source=source.replace("[(neck-.25,.305,.19),(neck-.13,.29,.18),(neck-.015,.19,.115),(neck+.015,.09,.083)]", "[(neck-.43,.255,.175),(neck-.31,.275,.175),(neck-.17,.265,.155),(neck-.045,.19,.115),(neck+.012,.091,.083)]")
source=source.replace("[(neck-.25,.308,.193),(neck-.235,.307,.192)]", "[(neck-.43,.258,.178),(neck-.415,.258,.178)]")
source=source.replace("(.018,.025,.032) if mystic", "(.055,.075,.082) if mystic")
source=source.replace('dist=.00005','dist=.004')
source=source.replace('scene.render.threads=4','scene.render.threads=2')
source=source.replace("            for o in PARTS[-2:]:\n                o.vertex_groups.clear();g=o.vertex_groups.new(name='Bip01 Spine2');g.add(list(range(len(o.data.vertices))),1,'REPLACE')", "            # Mantle retains barycentrically transferred shoulder/spine weights.")
exec(compile(source,str(HERE.parent/'create_tailored_characters.py'),'exec'),globals())
# Reuse the original exact-anatomy preparation without its outfit design.
prefix=source[source.index('def create(character,style):'):source.index("    hip=BONES['Bip01 Pelvis'].z;neck=BONES['Bip01 Neck'].z",source.index('def create(character,style):'))]
exec(prefix.replace('def create(character,style):','def prepare(character,style):'),globals())

def custom(character,style):
    prepare(character,style)
    hip=BONES['Bip01 Pelvis'].z;neck=BONES['Bip01 Neck'].z
    companion=style=='CafeHost';guard=style=='RomanGuard'
    cloth=tinted(style+' woven cloth',(.20,.33,.30) if companion else (.67,.57,.39),rough=.86)
    trim=tinted(style+' edge binding',(.10,.17,.22) if companion else (.34,.07,.045),rough=.8)
    pants=tinted(style+' continuous lower cloth',(.14,.19,.22) if companion else (.30,.25,.18),rough=.88)
    leather=tinted(style+' soft leather',(.075,.055,.037),rough=.65)
    metal=tinted(style+' brushed bronze',(.32,.23,.115),rough=.48,metal=.65)
    skinmat=tinted(style+' linen undersleeve',(.42,.35,.25),rough=.88)
    torso=[];lower=[];shoes=[];scalp=[]
    eye=BONES['Bip01 REye'].z
    for fi,(tri,(sub,ordinal)) in enumerate(zip(FACES,indices_global())):
        pts=[POINTS[i] for i in tri];z=sum(p.z for p in pts)/3
        name=DATA['surfaces'][sub]['name'].lower()
        hand=max(sum(w for b,w in zip(DATA['weights'][i]['bones'],DATA['weights'][i]['values']) if any(v in DATA['skeleton'][b]['name'] for v in (' Hand','Finger'))) for i in tri)
        if '_body' in name and hand<.35:
            COVERAGE.append({'submesh':sub,'triangle':ordinal})
            if z>hip+.01:torso.append((fi,tri))
            elif z<.18:shoes.append((fi,tri))
            else:lower.append((fi,tri))
        # Remove chef headwear and any old tall cap, retaining the facial mesh.
        elif (companion and z>eye+.08) or (guard and z>eye+.065):
            COVERAGE.append({'submesh':sub,'triangle':ordinal})
            if companion and '_head' in name and z<eye+.17:scalp.append((fi,tri))
    if not companion:
        torso=[(fi,tri) for fi,tri in torso if sum(POINTS[i].z for i in tri)/3<neck-.06 or abs(sum(POINTS[i].x for i in tri)/3)>.24]
    surface('Fitted cardigan and continuous sleeves' if companion else 'Fitted woven tunic and sleeves',torso,cloth,.010,smooth=False)
    if not companion:
        leg=surface('Continuous linen leggings and shaped footwear',lower+shoes,pants,.008,smooth=False)
        leg.data.update()
        leg.data.materials.append(leather)
        for face in leg.data.polygons:
            face.material_index=1 if face.center.z<.24 else 0
    else:surface('Soft shaped footwear',shoes,leather,.009,smooth=False)
    if companion:
        shell('Pleated casual midi skirt',[(hip-.57,.29,.215),(hip-.44,.275,.208),(hip-.27,.245,.19),(hip-.08,.207,.165),(hip+.06,.193,.15)],pants,pleats=.007)
        calf=tinted('Maya warm skin at ankle',(.43,.29,.21),rough=.8)
        for side in (-1,1):
            shell('Anatomically tapered lower leg',[(.15,.032,.043),(.21,.035,.045),(.28,.045,.053),(.38,.054,.057),(.46,.044,.05)],calf,offset=(side*.085,.008,0))
        shell('Cardigan ribbed hem',[(hip+.025,.202,.16),(hip+.065,.199,.158)],trim,pleats=.0015)
        for side in (-1,1):ribbon('Cardigan knitted opening',[(side*.038,neck-.025),(side*.045,neck-.18),(side*.035,hip+.06)],.019,trim)
        for z in (hip+.13,hip+.23,hip+.33):ellipsoid('Small cardigan button',front(.028,z,.032),(.0045,.003,.0045),metal)
        hair=tinted('Maya chestnut casual hair',(.075,.035,.018),rough=.78)
        if scalp:surface('Native fitted hair cap',scalp,hair,.007,smooth=False)
        for side in (-1,1):
            curve('Shaped side swept hair',[(side*.075,.012,eye+.14),(side*.10,.025,eye+.09),(side*.105,.04,eye-.04),(side*.08,.06,neck+.015)],.022,hair,'Bip01 Head')
        shell('Casual soft collar',[(neck-.035,.094,.085),(neck+.005,.087,.078)],trim)
    else:
        shell('Soft continuous tunic neckline',[(neck-.15,.245,.17),(neck-.08,.214,.148),(neck-.035,.12,.098),(neck-.012,.085,.076)],cloth,pleats=.0015)
        # A continuous curved, pleated garment replaces hip/torso boxes.
        shell('Draped knee length tunic',[(hip-.37,.264,.205),(hip-.26,.25,.197),(hip-.11,.23,.185),(hip+.06,.215,.18),(hip+.16,.211,.177)],cloth,pleats=.006)
        shell('Tunic stitched lower hem',[(hip-.37,.266,.207),(hip-.345,.265,.207)],trim,pleats=.006)
        shell('Flexible leather belt',[(hip+.075,.225,.19),(hip+.118,.223,.188)],leather)
        pts=[(.12-.23*i/18,neck-.04-(neck-hip-.12)*i/18) for i in range(19)]
        ribbon('Draped diagonal woven sash',pts,.075,trim)
        if guard:
            head='Bip01 Head';top=eye+.125
            helm=shell('Curved bronze helmet',[(eye+.070,.132,.125),(eye+.10,.13,.124),(eye+.155,.105,.103),(eye+.185,.045,.055)],metal)
            helm.vertex_groups.clear();g=helm.vertex_groups.new(name=head);g.add(list(range(len(helm.data.vertices))),1,'REPLACE')
            for side in (-1,1):curve('Curved helmet cheek guard',[(side*.112,-.015,eye+.002),(side*.116,-.045,eye-.055),(side*.09,-.06,eye-.13)],.017,metal,head)
            curve('Helmet crest support',[(0,.065,top+.03),(0,0,top+.065),(0,-.065,top+.035)],.012,trim,head)
    hidden={(c['submesh'],c['triangle']) for c in COVERAGE}
    bm=bmesh.new();bm.from_mesh(BODY.data);bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i,pair in enumerate(indices_global()) if pair in hidden],context='FACES')
    bm.to_mesh(BODY.data);bm.free()
    result=export(style)
    render(style)
    receipt={'style':style,'character':character,'triangles':sum(len(p['triangles'])//3 for p in result['parts']),
             'coveredSourceTriangles':len(COVERAGE),'bones':len(BONE_IDS),'license':'Original authored garments; MIT Rocketbox-derived fitted anatomy',
             'facialSource':'Original blendshapes retained by Unity builder','sha256':hashlib.sha256((DEST/(style+'.json')).read_bytes()).hexdigest()}
    (DEST/'receipt.json').write_text(json.dumps(receipt,indent=2));print('FINAL_CAST_READY',json.dumps(receipt),flush=True)

def indices_global():
    return [(sub,i//3) for sub,s in enumerate(DATA['surfaces']) for i in range(0,len(s['triangles']),3)]

selection=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for char,style in [('Kai','OpeningHero'),('Orion','MuseumMystic'),('Theo','Professor')]:
    if style in selection:create(char,style)
for char,style in [('Marcus','RomanCitizen'),('Orion','RomanGuard'),('Maya','CafeHost')]:
    if style in selection:custom(char,style)

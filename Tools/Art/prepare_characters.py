"""Convert source textures without modifying the Rocketbox FBX, inspect rigs, render approval views."""
import bpy, pathlib, json, math, shutil,sys
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2]
SOURCE=pathlib.Path(r'D:\CodexAssets\Rocketbox')
OUT=pathlib.Path(r'D:\LumenProject\Art\Characters') if '--new-hosts' in sys.argv else pathlib.Path(r'D:\LumenArtifacts\Characters'); OUT.mkdir(parents=True,exist_ok=True)
DATA={}
characters=[('Kai','Male_Adult_09'),('Orion','Male_Adult_04'),('Theo','Male_Adult_07')] if '--new-hosts' in sys.argv else [('Ada','Business_Female_02'),('Maya','Chef_Female_01'),('Marcus','Male_Adult_14')]
for character,folder in characters:
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    src=SOURCE/folder; dest=OUT/character; dest.mkdir(exist_ok=True)
    fbx=next(src.glob('*.fbx')); shutil.copy2(fbx,dest/(character+'.fbx'))
    for path in src.glob('*.tga'):
        im=bpy.data.images.load(str(path),check_existing=False)
        im.scale(min(im.size[0],2048),min(im.size[1],2048))
        im.filepath_raw=str(dest/(path.stem+'.png')); im.file_format='PNG'; im.save()
    bpy.ops.import_scene.fbx(filepath=str(fbx))
    objects=list(bpy.context.scene.objects); mats={}
    for ob in objects:
        if ob.type!='MESH': continue
        for mat in ob.data.materials:
            prefix=mat.name.split('.')[0].replace('_A_head','_head')
            files=list(dest.glob(prefix+'*color*.png'))
            if not files: files=list(dest.glob(prefix.replace('opacity','opacity')+'*.png'))
            tex=files[0] if files else None
            nodes=mat.node_tree.nodes; shader=nodes.get('Principled BSDF')
            if not shader: continue
            # Remove stale relative FBX texture links, use the normalized PNGs.
            for link in list(mat.node_tree.links): mat.node_tree.links.remove(link)
            output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
            mat.node_tree.links.new(shader.outputs['BSDF'],output.inputs['Surface'])
            shader.inputs['Roughness'].default_value=.58
            if tex:
                node=nodes.new('ShaderNodeTexImage'); node.image=bpy.data.images.load(str(tex))
                mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
                if 'opacity' in prefix or 'glasses' in prefix:
                    mat.node_tree.links.new(node.outputs['Alpha'],shader.inputs['Alpha'])
            mats[prefix]={'colorTexture':tex.name if tex else None,'normalTexture':next((p.name for p in dest.glob(prefix+'*normal*.png')),None),'cutout': 'opacity' in prefix or 'glasses' in prefix}
    DATA[character]={'sourceFolder':folder,'materials':mats,'bones':[b.name for o in objects if o.type=='ARMATURE' for b in o.data.bones],
                     'blendshapes':[k.name for o in objects if o.type=='MESH' and o.data.shape_keys for k in o.data.shape_keys.key_blocks],
                     'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects if o.type=='MESH')}
    # A pose suitable for visual assessment; export source remains untouched.
    for ob in objects:
        if ob.type=='ARMATURE':
            for side,angle in [('L',.52),('R',-.52)]:
                b=ob.pose.bones.get('Bip01 '+side+' UpperArm')
                if b: b.rotation_mode='XYZ'; b.rotation_euler.y=angle
    scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED'; scene.render.threads=4
    scene.render.resolution_x=900; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
    scene.world.color=(.25,.25,.25)
    bpy.ops.mesh.primitive_plane_add(size=200); floor=bpy.context.object
    fm=bpy.data.materials.new('Preview charcoal'); fm.diffuse_color=(.025,.04,.05,1); floor.data.materials.append(fm)
    for pos,power,size in [((2,-4,4),650,4),((-3,-2,2),450,3),((1,3,3),800,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.size=size
        light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=(2,-5,2)); cam=bpy.context.object
    cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=2.2; scene.camera=cam
    scene.render.filepath=str(OUT/(character+'.png')); bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(character+'.blend')))
    bpy.ops.render.render(write_still=True)
    print('CHARACTER_READY',character,DATA[character]['triangles'],flush=True)
(OUT/'characters.json').write_text(json.dumps(DATA,indent=2))
shutil.copy2(SOURCE/'LICENSE.md',OUT/'LICENSE.md')

import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[3]/'Art/Exhibits'
for name in ('TheropodSkeleton','HornedDinosaurSkeleton','ArenaPracticeSword'):
    bpy.ops.wm.open_mainfile(filepath=str(root/(name+'.blend')))
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=2
    scene.render.resolution_x=1100;scene.render.resolution_y=750;scene.render.resolution_percentage=100
    scene.world.color=(.18,.20,.22)
    target=Vector((-.35,0,1.2)) if name!='ArenaPracticeSword' else Vector((0,0,.28))
    for pos,power,size in [((0,-4,5),600,5),((-3,2,3),350,4),((3,1,4),500,3)]:
        bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size
        light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=(2.4,-7,3.3) if name!='ArenaPracticeSword' else (1,-2,1.1));cam=bpy.context.object
    cam.data.type='ORTHO';cam.data.ortho_scale=6.5 if name!='ArenaPracticeSword' else .98
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
    scene.view_settings.view_transform='AgX';scene.render.filepath=str(root/(name+'-review.png'));bpy.ops.render.render(write_still=True)

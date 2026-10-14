"""Turns an exported set (Assets/OhmsLaw/Editor/SetExport.cs) into a .blend to edit and an .fbx that re-imports into Unity in place.
Usage: blender -b --python Tools/obj_to_blend.py -- <in.obj> <out.blend> <out.fbx>"""
import sys

import bpy

obj, blend, fbx = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=obj)  # OBJ Y-up -> Blender Z-up
for ob in bpy.context.scene.objects:  # the exporter wrote hierarchy paths as names; keep the last part readable
    ob.name = ob.name.split("/")[-1][:60] or ob.name
bpy.ops.file.pack_all()  # textures inside the .blend: it opens anywhere
bpy.ops.wm.save_as_mainfile(filepath=blend)
# Forward -Z, up Y with the space transform baked: Unity's importer lands every object where it was exported from.
bpy.ops.export_scene.fbx(filepath=fbx, axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL",
                         bake_space_transform=True, path_mode="COPY", embed_textures=False, mesh_smooth_type="FACE")
print("OK", blend, fbx, len(bpy.context.scene.objects), "objects")

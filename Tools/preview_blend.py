"""Workbench render of a .blend from a Unity camera pose, to check an export (Unity (x, y, z) is Blender (-x, -z, y)).
Usage: blender -b <file.blend> --python Tools/preview_blend.py -- <out.png> <cam ux,uy,uz> <look ux,uy,uz> <vertical fov>"""
import sys

import bpy
from mathutils import Vector

out, cam, look, fov = sys.argv[sys.argv.index("--") + 1:]
to_blender = lambda s: (lambda x, y, z: Vector((-x, -z, y)))(*map(float, s.split(",")))
scene = bpy.context.scene
data = bpy.data.cameras.new("Preview")
data.sensor_fit, data.angle_y = "VERTICAL", float(fov) * 3.14159265 / 180
camera = bpy.data.objects.new("Preview", data)
scene.collection.objects.link(camera)
camera.location = to_blender(cam)
camera.rotation_euler = (to_blender(look) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = camera
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "TEXTURE"
scene.display.shading.light = "STUDIO"
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
scene.render.filepath = out
bpy.ops.render.render(write_still=True)

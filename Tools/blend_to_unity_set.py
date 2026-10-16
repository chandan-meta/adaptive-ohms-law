"""Exports the set you built in Blender on top of an exported wire (Exports/InsideTheWire/InsideTheWire.blend) for Unity:
every object except the module's own code-built pieces (the conduit, its bands and ring, the badges: Unity builds those, the
lesson drives them) -> <out>/WireSet.fbx, the images its materials need -> <out>/Textures/, the materials' values ->
<out>/WireSet.json (Editor/WireSet.cs builds URP Lit materials from them). Coordinates round-trip: Unity places the set
where it is in Blender.
Material support: base colour (image, image x tint, or a colour), metallic / roughness (values or maps, packed into URP's
metallic-smoothness map), normal map (green flipped when the graph inverts it, as for DirectX maps), opacity (packed into
the base map's alpha, alpha-clipped), UV tiling from a Mapping node, emission colour x strength. Procedural colours (bevel,
AO, grunge mixes) export as a light metal colour.
Optional set config (JSON, second argument; Tools/sets/*.json): "name" (file stem, default WireSet), "exclude" (regex of
the module's own pieces), "names" (object-name prefix -> name), "rename_by_material" (Maya-style polySurface12 / pCube3
named after their material), "max_distance" (objects whose centre is further from the origin are left out and listed:
mis-scaled imports), "max_size" (textures halved until they fit), "glow" (material -> {"rule": screen | blue | white |
blue+white, "strength": HDR}: an emission map made from the base map's bright saturated / cyan-blue / white areas).
Usage: blender -b <file.blend> --python Tools/blend_to_unity_set.py -- <out folder> [config.json]"""
import bpy, json, os, re, sys
import numpy as np
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out = args[0]
cfg = json.load(open(args[1], encoding="utf-8")) if len(args) > 1 else {}
os.makedirs(os.path.join(out, "Textures"), exist_ok=True)
safe = lambda s: re.sub(r"[^A-Za-z0-9_.-]+", "_", s.strip()).strip("_")
stem = cfg.get("name", "WireSet")
# The pieces Tools/obj_to_blend.py brought in from the module (their names after its renaming): Unity keeps building these.
code_built = re.compile(cfg.get("exclude", r"^(Conduit|Mesh|Face|Glyph|Halo|Puck|Rim|Floor|Block|Cap|Edge_Light|Cable|Copper_Lattice)([._].*)?$"))
names = cfg.get("names", {"aed17867": "Terminal Machine", "tripo_40782d13": "Wall Panels", "tripo_65e94f68": "Space Ring",
                          "tripo_7985deb1": "Circular Platform", "G_0": "Floor Ring", "Circle": "Floor Disc", "Cylinder": "Floor Grating"})
maya = re.compile(r"^(polySurface|pCube|pCylinder|pTorus|pSphere|pPlane|pCone)\d")


def centre(ob):
    pts = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    return sum(pts, Vector()) / 8


picked = [ob for ob in bpy.data.objects if ob.type == "MESH" and len(ob.data.vertices) > 0 and not code_built.match(ob.name)]
if "max_distance" in cfg:
    far = [ob for ob in picked if centre(ob).length > cfg["max_distance"]]
    for ob in far:
        print("SKIP far from the set:", ob.name, [round(v) for v in centre(ob)])
    picked = [ob for ob in picked if ob not in far]
bpy.ops.object.select_all(action="DESELECT")
counts = {}
for ob in picked:
    world = ob.matrix_world.copy()
    ob.animation_data_clear()  # a static set: an FBX take left on a part re-keys it once unparented (one landed 440 m away)
    ob.parent = None
    ob.matrix_world = world  # parents (RootNode empties) are not exported
    base = next((v for k, v in names.items() if ob.name.startswith(k)), None)
    if not base and cfg.get("rename_by_material") and maya.match(ob.name) and ob.material_slots and ob.material_slots[0].material:
        base = safe(ob.material_slots[0].material.name)
    base = base or safe(ob.name)
    counts[base] = counts.get(base, 0) + 1
    ob.name = f"{base} {counts[base]}"
    ob.select_set(True)


def linked_node(socket):
    return socket.links[0].from_node if socket and socket.links else None


def image_behind(socket, flips=None):
    """The image feeding a socket through any chain of nodes; flips collects INVERT nodes met on the way."""
    node = linked_node(socket)
    if not node:
        return None
    if node.type == "TEX_IMAGE":
        return node.image if node.image and node.image.size[0] else None  # no pixels (a map the kit never shipped): no map
    if node.type == "INVERT" and flips is not None:
        flips.append(node)
    for i in node.inputs:
        img = image_behind(i, flips) if i.is_linked else None
        if img:
            return img
    return None


def base_colour(socket):
    """(image, tint) for an image or an image x tint; (None, colour) for a plain colour or a multiply of two colours;
    (None, None) for a procedural colour."""
    node = linked_node(socket)
    if not node:
        return None, list(socket.default_value)
    if node.type == "TEX_IMAGE" and node.image and node.image.size[0]:
        return node.image, None
    if node.type == "MIX" and node.blend_type == "MULTIPLY":
        a = next((i for i in node.inputs if i.name == "A" and i.type == "RGBA"), None)
        b = next((i for i in node.inputs if i.name == "B" and i.type == "RGBA"), None)
        if a and b and not b.is_linked:
            if a.links and a.links[0].from_node.type == "TEX_IMAGE":
                return a.links[0].from_node.image, list(b.default_value)
            if not a.is_linked:
                return None, [x * y for x, y in zip(a.default_value, b.default_value)]
    return None, None


def pixels(img):
    a = np.empty(img.size[0] * img.size[1] * 4, np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(img.size[1], img.size[0], 4)


def save_png(arr, path):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels.foreach_set(np.ascontiguousarray(arr, np.float32).ravel())
    im.filepath_raw, im.file_format = path, "PNG"
    im.save()
    bpy.data.images.remove(im)


def resized(arr, shape):
    """Nearest-neighbour resize, so maps of different sizes pack together."""
    h, w = shape
    ys = (np.arange(h) * arr.shape[0] / h).astype(int)
    xs = (np.arange(w) * arr.shape[1] / w).astype(int)
    return arr[ys][:, xs]


written = {}


def shrunk(arr):
    """Halved (2x2 means) until it fits max_size: Unity caps set textures at 2048 anyway."""
    limit = cfg.get("max_size")
    while limit and max(arr.shape[:2]) > limit and arr.shape[0] % 2 == 0 and arr.shape[1] % 2 == 0:
        h, w = arr.shape[:2]
        arr = arr.reshape(h // 2, 2, w // 2, 2, arr.shape[2]).mean(axis=(1, 3))
    return arr


def write_image(img):
    """The image as it is (packed bytes or a save), named after it with a proper extension; a halved PNG over max_size."""
    if img.name in written:
        return written[img.name]
    ext = {"JPEG": ".jpg", "PNG": ".png", "TARGA": ".tga"}.get(img.file_format, ".png")
    key = safe(re.sub(r"\.(jpe?g|png|tga)", "", img.name, flags=re.I)) + ext  # "x.jpg.001" -> "x.001.jpg"
    if cfg.get("max_size") and max(img.size) > cfg["max_size"]:
        key = key.rsplit(".", 1)[0] + ".png"
        save_png(shrunk(pixels(img)), os.path.join(out, "Textures", key))
        written[img.name] = key
        return key
    path = os.path.join(out, "Textures", key)
    if img.packed_file:
        open(path, "wb").write(img.packed_file.data)
    else:
        img.save(filepath=path)
    written[img.name] = key
    return key


def srgb(colour):
    """Linear (Blender) to sRGB (Unity's Material.SetColor) for the RGB channels; HDR values past 1 by the 2.2 power
    Unity inverts. Without it a plain Blender colour lands much darker in Unity (a green board went near black)."""
    to = lambda x: 12.92 * x if x <= 0.0031308 else (1.055 * x ** (1 / 2.4) - 0.055 if x <= 1 else x ** (1 / 2.2))
    return [round(to(max(0.0, c)), 4) for c in colour[:3]] + list(colour[3:])


def ramp(x, a, b):
    return np.clip((x - a) / (b - a), 0, 1)


def glow_map(img, rule):
    """Emission map from the base map: the pixels that are lights in this kind of atlas (base colour there, black elsewhere)."""
    rgb = shrunk(pixels(img))[..., :3]
    mx, mn = rgb.max(-1), rgb.min(-1)
    s = (mx - mn) / (mx + 1e-6)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hue = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    blue = ramp(hue, 165, 180) * (1 - ramp(hue, 225, 240)) * ramp(s, 0.2, 0.35) * ramp(mx, 0.45, 0.65)
    white = ramp(mx, 0.93, 0.98) * (1 - ramp(s, 0.04, 0.1))
    screen = ramp(s, 0.3, 0.5) * ramp(mx, 0.3, 0.5)
    m = {"blue": blue, "white": white, "blue+white": np.maximum(blue, white), "screen": screen}[rule]
    rgba = np.ones(rgb.shape[:2] + (4,), np.float32)
    rgba[..., :3] = rgb * m[..., None]
    return rgba, float(m.mean())


materials = {}
for ob in picked:
    for slot in ob.material_slots:
        m = slot.material
        if not m or safe(m.name) in materials:
            continue
        m.name = safe(m.name)  # FBX material names must match the Unity remap exactly (no trailing spaces, no '+')
        spec = {"name": m.name, "baseMap": "", "baseColor": [0.8, 0.8, 0.8, 1], "metallic": 0.0, "roughness": 0.5, "emission": [0, 0, 0],
                "normalMap": "", "maskMap": "", "alphaClip": False, "tiling": [1, 1]}
        bsdf = next((n for n in (m.node_tree.nodes if m.use_nodes else []) if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf:
            img, tint = base_colour(bsdf.inputs["Base Color"])
            opacity = image_behind(bsdf.inputs["Alpha"])
            if img and opacity:  # the cut-outs ride in the base map's alpha
                link = bsdf.inputs["Alpha"].links[0]
                own = link.from_node.type == "TEX_IMAGE" and link.from_socket.name == "Alpha"  # the image's own alpha, not a grey mask
                rgba = pixels(img)
                rgba[..., 3] = resized(pixels(opacity), rgba.shape[:2])[..., 3 if own else 0]
                cut = rgba[..., 3] < 0.5
                if cut.any() and not cut.all():
                    rgba[cut, :3] = rgba[~cut, :3].mean(0)  # no white card bleeding into the edges and the mips
                spec["baseMap"] = safe(re.sub(r"\.(jpe?g|png)", "", img.name, flags=re.I)) + "_alpha.png"
                save_png(shrunk(rgba), os.path.join(out, "Textures", spec["baseMap"]))
                spec["alphaClip"] = True
            elif img:
                spec["baseMap"] = write_image(img)
            tint = srgb(tint) if tint else None  # Blender's colours are linear; Unity's material colours are sRGB
            if img:
                spec["baseColor"] = tint or [1, 1, 1, 1]
            else:
                spec["baseColor"] = tint or [0.85, 0.85, 0.87, 1]  # a colour, or a procedural one: a light metal
            spec["metallic"] = round(bsdf.inputs["Metallic"].default_value, 3)
            spec["roughness"] = round(bsdf.inputs["Roughness"].default_value, 3)
            metal_map, rough_map = image_behind(bsdf.inputs["Metallic"]), image_behind(bsdf.inputs["Roughness"])
            if metal_map or rough_map:  # URP: R metallic, A smoothness
                ref = shrunk(pixels(metal_map or rough_map))
                mask = np.zeros_like(ref)
                mask[..., 0] = ref[..., 0] if metal_map else spec["metallic"]
                mask[..., 3] = 1 - (resized(pixels(rough_map), ref.shape[:2])[..., 0] if rough_map else spec["roughness"])
                spec["maskMap"] = safe(m.name) + "_metallic_smoothness.png"
                save_png(mask, os.path.join(out, "Textures", spec["maskMap"]))
            normal_node = linked_node(bsdf.inputs["Normal"])
            if normal_node and normal_node.type == "NORMAL_MAP":
                flips = []
                nimg = image_behind(normal_node.inputs["Color"], flips)
                if nimg:
                    n = pixels(nimg)
                    if flips:
                        n[..., 1] = 1 - n[..., 1]  # the graph flips green (a DirectX map): Unity wants the flipped one too
                    n[..., 3] = 1
                    n = shrunk(n)
                    spec["normalMap"] = safe(re.sub(r"\.(jpe?g|png)", "", nimg.name, flags=re.I)) + "_gl.png"
                    save_png(n, os.path.join(out, "Textures", spec["normalMap"]))
            strength = bsdf.inputs["Emission Strength"].default_value
            if strength > 0 and not bsdf.inputs["Emission Color"].is_linked:
                spec["emission"] = srgb([c * strength for c in list(bsdf.inputs["Emission Color"].default_value)[:3]])
            glow = cfg.get("glow", {}).get(m.name)
            if glow and img:
                emap, share = glow_map(img, glow["rule"])
                spec["emissionMap"] = safe(m.name) + "_emission.png"
                save_png(emap, os.path.join(out, "Textures", spec["emissionMap"]))
                spec["emission"] = [glow.get("strength", 1.5)] * 3
                print(f"GLOW {m.name}: {glow['rule']} x{glow.get('strength', 1.5)}, {share:.1%} of the map lit")
            mapping = next((n for n in m.node_tree.nodes if n.type == "MAPPING"), None)
            if mapping:
                scale_in = mapping.inputs["Scale"]
                src = linked_node(scale_in)
                s = [src.outputs[0].default_value] * 2 if src and src.type == "VALUE" else list(scale_in.default_value)[:2]
                spec["tiling"] = [round(s[0], 3), round(s[1], 3)]
        materials[m.name] = spec

json.dump({"materials": list(materials.values())}, open(os.path.join(out, stem + ".json"), "w"), indent=1)
bpy.ops.export_scene.fbx(filepath=os.path.join(out, stem + ".fbx"), use_selection=True, object_types={"MESH"},
                         axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                         use_mesh_modifiers=True, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False)
print("SET", len(picked), "objects", len(materials), "materials:", ", ".join(sorted(o.name for o in picked)))

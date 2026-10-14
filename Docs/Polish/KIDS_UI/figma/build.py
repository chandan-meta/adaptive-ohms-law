"""Builds the Kids UI Figma build in three forms from lib.js + the part files:
  build/bundle.js            everything, for mock.js (node mock.js)
  plugin/{manifest.json, code.js, ui.html}
                             a Figma development plugin: Plugins > Development > Import plugin from manifest, open the
                             Ohm's Law file, run it. The UI carries the gameplay frames; it audits the file, builds, and
                             (when receiver.py is running) sends the audit and PNG renders back to Logs/UI_V5/figma/.
  build/mcp/<part>.js        one use_figma call per part (IMG_HASHES filled after upload_assets).
It also prepares the images (desktop / tablet / before frames, the Wattson portrait, the reference sheet) in images/.
Usage: python build.py"""
import base64, io, json, os, re, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "..", "..", ".."))
SHOTS = os.path.join(ROOT, "Logs", "UI_V5", "backdrops")
PARTS = ["1_foundations.js", "2_icons.js", "3a_controls.js", "3b_panels.js", "4_screens.js", "5_before_after.js", "6_sheet.js", "run.js"]
# Mockup key -> (scene folder, frame) in the HUD-free captures; "before" uses the same frame with the v5 HUD.
FRAMES = {
    "historical": ("Scene04_MeetWattson", "04_S04_L02.png"),
    "conductor": ("Scene05_InsideTheWire", "09_gate_S05_ApplyVoltage.png"),
    "experiment": ("Scene10_Experiment", "09_gate_S10_Set3V.png"),
    "graph": ("Scene11_Graph", "02_gate_S11_DrawLine.png"),
    "materials": ("Scene12_Resistance", "10_gate_S12_WhichResists.png"),
    "quiz": ("Scene10_Experiment", "11_gate_S10_After3V.png"),
    "complete": ("Scene13_OhmsLaw", "09_end_card.png"),
}
WATTSON = ("Scene04_MeetWattson", "03_S04_L01.png", (700, 150, 1200, 650))  # crop box (desktop frame): his face and hat
REFERENCE = os.path.join(HERE, "..", "reference.png")


def read(name):
    return open(os.path.join(HERE, name), encoding="utf-8").read()


def jpeg(src, size, quality=82):
    im = Image.open(src).convert("RGB")
    if size and im.size != size:
        im = im.resize(size, Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, "JPEG", quality=quality, optimize=True, progressive=True)
    return buf.getvalue()


def images():
    out = {}
    for key, (scene, frame) in FRAMES.items():
        for kind, folder, size in (("d", "desktop", (1920, 1080)), ("t", "tablet", (1536, 1152)), ("before", "before", (1600, 900))):
            src = os.path.join(SHOTS, folder, scene, frame)
            if not os.path.exists(src):
                sys.exit("missing frame " + src)
            out[f"{kind}_{key}"] = jpeg(src, size, 80 if kind != "before" else 78)
    scene, frame, box = WATTSON
    im = Image.open(os.path.join(SHOTS, "desktop", scene, frame)).convert("RGB").crop(box).resize((320, 320), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, "JPEG", quality=88); out["wattson"] = buf.getvalue()
    if os.path.exists(REFERENCE):
        out["reference"] = jpeg(REFERENCE, None, 85)
    os.makedirs(os.path.join(HERE, "images"), exist_ok=True)
    for k, v in out.items():
        open(os.path.join(HERE, "images", k + ".jpg"), "wb").write(v)
    return out


def minify(js):
    """Comments and indentation only: safe for this code (no regex literals or strings with //)."""
    lines = []
    for line in js.splitlines():
        s = line.strip()
        if not s or s.startswith("//"):
            continue
        lines.append(s)
    return "\n".join(lines)


def main():
    lib = read("lib.js")
    parts = {p: read(p) for p in PARTS}
    bundle = lib + "\n" + "\n".join(parts[p] for p in PARTS)
    os.makedirs(os.path.join(HERE, "build", "mcp"), exist_ok=True)
    open(os.path.join(HERE, "build", "bundle.js"), "w", encoding="utf-8").write(bundle)

    # use_figma calls: lib + one part (minified), each under the 50 000-character limit.
    calls = {"1_foundations": "await foundations(); await foundationsBoard();", "2_icons": "await icons();", "3a_controls": "await controls();",
             "3b_panels": "await panels();", "4_screens": "await screens();", "5_before_after": "await beforeAfter();", "6_sheet": "await sheet();"}
    for name, call in calls.items():
        nl = chr(10)
        code = "const IMG_HASHES = __IMG_HASHES__;" + nl + minify(lib + nl + parts[name + ".js"]) + nl + "await init();" + nl + call + nl + "return LOG.join(String.fromCharCode(10));"
        open(os.path.join(HERE, "build", "mcp", name + ".js"), "w", encoding="utf-8").write(code)
        print(f"use_figma {name}: {len(code)} chars" + ("  ** OVER 50 000 **" if len(code) > 50000 else ""))

    imgs = images()
    plugin = os.path.join(HERE, "plugin")
    os.makedirs(plugin, exist_ok=True)
    json.dump({"name": "Ohm's Law Kids UI", "id": "ohmslaw-kids-ui-dev", "api": "1.0.0", "main": "code.js", "ui": "ui.html",
               "editorType": ["figma"], "documentAccess": "dynamic-page",
               "networkAccess": {"allowedDomains": ["none"], "devAllowedDomains": ["http://localhost:8787"]}},
              open(os.path.join(plugin, "manifest.json"), "w"), indent=2)
    code = "var IMG_HASHES = {};\n" + bundle + "\n" + read("plugin_main.js")
    open(os.path.join(plugin, "code.js"), "w", encoding="utf-8").write(code)
    data = {k: "data:image/jpeg;base64," + base64.b64encode(v).decode() for k, v in imgs.items()}
    ui = read("plugin_ui.html").replace("/*__IMAGES__*/{}", json.dumps(data))
    open(os.path.join(plugin, "ui.html"), "w", encoding="utf-8").write(ui)
    total = sum(len(v) for v in imgs.values())
    print(f"plugin: code.js {len(code)//1024} KB, ui.html {os.path.getsize(os.path.join(plugin, 'ui.html'))//1024} KB, {len(imgs)} images {total//1024} KB")


if __name__ == "__main__":
    main()

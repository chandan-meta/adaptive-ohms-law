"""Copy the downloaded CC0 / Mixkit audio into Assets/OhmsLaw/Audio with semantic names, measure each clip's level,
compute a per-clip gain toward a per-category reference level, and write CueDefinitions/library.json (read by the Unity
editor menu "Ohm's Law/Audio/Build library"), the Licenses folder and the asset manifest.

  python Tools/audio_organize.py <download-dir>
"""
import json, os, re, shutil, subprocess, sys

dl = sys.argv[1]
root = os.path.join("Assets", "OhmsLaw", "Audio")
K = os.path.join(dl, "kenney")
ki, kd, ks, kj = (os.path.join(K, "kenney_interface-sounds", "Audio"), os.path.join(K, "kenney_digital-audio", "Audio"),
                  os.path.join(K, "kenney_sci-fi-sounds", "Audio"), os.path.join(K, "kenney_music-jingles", "Audio"))
oga, mk = os.path.join(dl, "oga"), os.path.join(dl, "mixkit")

SRC = {  # source labels for the manifest
    "oga_rony": ("RonyDkid", "CC0", "https://opengameart.org/content/intro-music-0"),
    "oga_minto": ("MintoDog", "CC0", "https://opengameart.org/content/magic-puzzle-in-game-1"),
    "oga_srg": ("SRG774", "CC0", "https://opengameart.org/content/dark-sci-fi-audio-pack"),
    "oga_vit": ("vitalezzz", "CC0", "https://opengameart.org/content/claimed-by-the-void"),
    "kenney_ui": ("Kenney", "CC0", "https://kenney.nl/assets/interface-sounds"),
    "kenney_dig": ("Kenney", "CC0", "https://kenney.nl/assets/digital-audio"),
    "kenney_sci": ("Kenney", "CC0", "https://kenney.nl/assets/sci-fi-sounds"),
    "kenney_jin": ("Kenney", "CC0", "https://kenney.nl/assets/music-jingles"),
    "mixkit": ("Mixkit (Envato)", "Mixkit Sound Effects Free License", "https://mixkit.co/license/"),
}
# name, category folder, source file, source key, mixer group, loop, reference level (LUFS / dB mean), original title
PICKS = [
    # music
    ("BGM_Professor_Ohm_Laboratory", "Music/Laboratory", os.path.join(oga, "game_2.mp3"), "oga_rony", "Music", True, -18, "Intro Music (game.mp3)"),
    ("BGM_Wattson_Magical_Discovery", "Music/Wattson", os.path.join(oga, "magic_puzzle_in-game_1_bpm110.ogg"), "oga_minto", "Music", True, -18, "Magic Puzzle In-Game 1 (bpm110)"),
    ("BGM_Conductor_Airy", "Music/Conductor", os.path.join(oga, "airy_0.ogg"), "oga_srg", "Music", True, -19, "Dark Sci-Fi Audio Pack: airy"),
    ("BGM_Conductor_Pulse", "Music/Conductor", os.path.join(oga, "pulse_0.ogg"), "oga_srg", "Music", True, -19, "Dark Sci-Fi Audio Pack: pulse"),
    ("BGM_Conductor_Mystery", "Music/Conductor", os.path.join(oga, "sector_0.ogg"), "oga_srg", "Music", True, -19, "Dark Sci-Fi Audio Pack: sector"),
    ("BGM_Electric_Current_Experiment", "Music/Experiment", os.path.join(oga, "claimed_by_the_void_loop.mp3"), "oga_vit", "Music", True, -19, "Claimed by the Void (loop)"),
    # success / jingles
    ("SFX_Discovery_Victory", "Music/Success", os.path.join(oga, "victory_5.ogg"), "oga_srg", "SFX", False, -15, "Dark Sci-Fi Audio Pack: victory"),
    ("SFX_Correct_Answer", "Music/Success", os.path.join(kj, "jingles_PIZZI01.ogg"), "kenney_jin", "SFX", False, -15, "jingles_PIZZI01"),
    ("SFX_Experiment_Complete", "Music/Success", os.path.join(kj, "jingles_STEEL07.ogg"), "kenney_jin", "SFX", False, -15, "jingles_STEEL07"),
    ("SFX_Scientific_Discovery", "Music/Success", os.path.join(kj, "jingles_PIZZI07.ogg"), "kenney_jin", "SFX", False, -15, "jingles_PIZZI07"),
    ("SFX_Final_Success", "Music/Success", os.path.join(kj, "jingles_STEEL14.ogg"), "kenney_jin", "SFX", False, -15, "jingles_STEEL14"),
    ("SFX_Magic_Success", "Music/Success", os.path.join(mk, "mixkit_2633.wav"), "mixkit", "SFX", False, -16, "Fantasy game success notification (2633)"),
    # ui
    ("UI_Click", "SFX/UI", os.path.join(ki, "click_001.ogg"), "kenney_ui", "UI", False, -18, "click_001"),
    ("UI_Select", "SFX/UI", os.path.join(ki, "select_001.ogg"), "kenney_ui", "UI", False, -18, "select_001"),
    ("UI_Back", "SFX/UI", os.path.join(ki, "back_001.ogg"), "kenney_ui", "UI", False, -18, "back_001"),
    ("UI_Open", "SFX/UI", os.path.join(ki, "open_001.ogg"), "kenney_ui", "UI", False, -18, "open_001"),
    ("UI_Close", "SFX/UI", os.path.join(ki, "close_001.ogg"), "kenney_ui", "UI", False, -18, "close_001"),
    ("UI_Confirm", "SFX/UI", os.path.join(ki, "confirmation_001.ogg"), "kenney_ui", "UI", False, -18, "confirmation_001"),
    ("UI_Error", "SFX/UI", os.path.join(ki, "error_006.ogg"), "kenney_ui", "UI", False, -20, "error_006"),
    ("UI_Toggle", "SFX/UI", os.path.join(ki, "toggle_001.ogg"), "kenney_ui", "UI", False, -18, "toggle_001"),
    ("UI_Question", "SFX/UI", os.path.join(ki, "question_001.ogg"), "kenney_ui", "UI", False, -18, "question_001"),
    ("UI_Hover", "SFX/UI", os.path.join(ki, "tick_001.ogg"), "kenney_ui", "UI", False, -24, "tick_001"),
    ("UI_Stamp", "SFX/UI", os.path.join(ki, "bong_001.ogg"), "kenney_ui", "UI", False, -17, "bong_001"),
    ("UI_Scroll", "SFX/UI", os.path.join(ki, "scroll_001.ogg"), "kenney_ui", "UI", False, -20, "scroll_001"),
    ("SFX_SciFi_UI_Hover", "SFX/UI", os.path.join(oga, "hover_0.ogg"), "oga_srg", "UI", False, -20, "Dark Sci-Fi Audio Pack: hover"),
    # character (Wattson)
    ("SFX_Wattson_Appear", "SFX/Character", os.path.join(mk, "mixkit_2638.wav"), "mixkit", "Character", False, -16, "Fairy magic sparkle (2638)"),
    ("SFX_Wattson_Whoosh", "SFX/Character", os.path.join(mk, "mixkit_2350.wav"), "mixkit", "Character", False, -18, "Magic sparkle whoosh (2350)"),
    ("SFX_Wattson_Wave", "SFX/Character", os.path.join(ki, "glass_002.ogg"), "kenney_ui", "Character", False, -18, "glass_002"),
    ("SFX_Wattson_Pop", "SFX/Character", os.path.join(kd, "pepSound1.ogg"), "kenney_dig", "Character", False, -18, "pepSound1"),
    ("SFX_Wattson_Settle", "SFX/Character", os.path.join(kd, "phaserDown1.ogg"), "kenney_dig", "Character", False, -20, "phaserDown1"),
    ("SFX_Wattson_Magic", "SFX/Character", os.path.join(mk, "mixkit_2352.wav"), "mixkit", "Character", False, -17, "Magic wand sparkle (2352)"),
    ("SFX_Magic_Gather", "SFX/Character", os.path.join(mk, "mixkit_3082.wav"), "mixkit", "Character", False, -17, "Magical light moving (3082)"),
    ("SFX_Magic_Poof", "SFX/Character", os.path.join(mk, "mixkit_2407.wav"), "mixkit", "Character", False, -17, "Magic sparkle poof hit (2407)"),
    # electrical
    ("SFX_Electric_Glitch", "SFX/Electrical", os.path.join(mk, "mixkit_2595.wav"), "mixkit", "Electrical", False, -18, "Small electric glitch (2595)"),
    ("SFX_Electric_Buzz", "SFX/Electrical", os.path.join(mk, "mixkit_2596.wav"), "mixkit", "Electrical", False, -18, "Electric buzz glitch (2596)"),
    ("SFX_Electric_Whoosh", "SFX/Electrical", os.path.join(mk, "mixkit_2600.wav"), "mixkit", "Electrical", False, -16, "Electric whoosh (2600)"),
    ("SFX_Electronics_PowerUp", "SFX/Electrical", os.path.join(mk, "mixkit_2599.wav"), "mixkit", "Electrical", False, -17, "Electronics power up (2599)"),
    ("SFX_Voltage_Reveal", "SFX/Electrical", os.path.join(mk, "mixkit_2602.wav"), "mixkit", "Electrical", False, -16, "Electricity static power up (2602)"),
    ("LOOP_Electric_Charge_Hum", "SFX/Electrical", os.path.join(mk, "mixkit_332.wav"), "mixkit", "Electrical", True, -22, "Electric charge hum (332)"),
    ("SFX_Static_Crackles", "SFX/Electrical", os.path.join(mk, "mixkit_2597.wav"), "mixkit", "Electrical", False, -18, "Static electric crackles (2597)"),
    ("LOOP_Electricity_Sparks", "SFX/Electrical", os.path.join(mk, "mixkit_2138.wav"), "mixkit", "Electrical", True, -22, "Futuristic electricity sparks (2138)"),
    ("LOOP_Electricity_Hum_Intense", "SFX/Electrical", os.path.join(mk, "mixkit_2593.wav"), "mixkit", "Electrical", True, -22, "Intense electricity hum (2593)"),
    ("LOOP_Electricity_Hum_Low", "SFX/Electrical", os.path.join(mk, "mixkit_2592.wav"), "mixkit", "Electrical", True, -23, "Low electricity humming (2592)"),
    ("SFX_Energy_Field", "SFX/Electrical", os.path.join(ks, "forceField_000.ogg"), "kenney_sci", "Electrical", False, -18, "forceField_000"),
    ("SFX_Energy_Field_Off", "SFX/Electrical", os.path.join(ks, "forceField_003.ogg"), "kenney_sci", "Electrical", False, -18, "forceField_003"),
    ("SFX_Terminal_Positive", "SFX/Electrical", os.path.join(kd, "powerUp3.ogg"), "kenney_dig", "Electrical", False, -18, "powerUp3"),
    ("SFX_Terminal_Negative", "SFX/Electrical", os.path.join(kd, "lowDown.ogg"), "kenney_dig", "Electrical", False, -18, "lowDown"),
    ("SFX_Battery_Connect", "SFX/Electrical", os.path.join(kd, "zapTwoTone.ogg"), "kenney_dig", "Electrical", False, -18, "zapTwoTone"),
    ("SFX_Current_Marker", "SFX/Electrical", os.path.join(kd, "twoTone1.ogg"), "kenney_dig", "Electrical", False, -18, "twoTone1"),
    ("SFX_Drift_Chime", "SFX/Electrical", os.path.join(kd, "threeTone1.ogg"), "kenney_dig", "Electrical", False, -18, "threeTone1"),
    ("SFX_Current_Reveal", "SFX/Electrical", os.path.join(kd, "powerUp12.ogg"), "kenney_dig", "Electrical", False, -17, "powerUp12"),
    # scanner / apparatus
    ("SFX_Scanner_Ping", "SFX/Scanner", os.path.join(kd, "tone1.ogg"), "kenney_dig", "SFX", False, -19, "tone1"),
    ("SFX_Scanner_Lock", "SFX/Scanner", os.path.join(kd, "highUp.ogg"), "kenney_dig", "SFX", False, -18, "highUp"),
    ("SFX_Scan_Charge", "SFX/Scanner", os.path.join(ks, "computerNoise_000.ogg"), "kenney_sci", "SFX", False, -20, "computerNoise_000"),
    ("SFX_Apparatus_Click", "SFX/Scanner", os.path.join(ki, "switch_002.ogg"), "kenney_ui", "SFX", False, -19, "switch_002"),
    # camera transitions
    ("SFX_Whoosh_Fast", "SFX/CameraTransitions", os.path.join(mk, "mixkit_1492.wav"), "mixkit", "SFX", False, -15, "Cinematic whoosh fast transition (1492)"),
    ("SFX_Tunnel_Woosh", "SFX/CameraTransitions", os.path.join(mk, "mixkit_2608.wav"), "mixkit", "SFX", False, -15, "Cinematic tunnel reverb woosh (2608)"),
    ("SFX_SciFi_Sweep", "SFX/CameraTransitions", os.path.join(mk, "mixkit_2408.wav"), "mixkit", "SFX", False, -16, "Fast sci fi transition sweep (2408)"),
    ("SFX_Magic_Sweep", "SFX/CameraTransitions", os.path.join(mk, "mixkit_869.wav"), "mixkit", "SFX", False, -16, "Magic transition sweep presentation (869)"),
    ("SFX_Tech_Alert", "SFX/CameraTransitions", os.path.join(mk, "mixkit_3111.wav"), "mixkit", "SFX", False, -18, "Technology alert transition (3111)"),
    ("SFX_Impact_Low", "SFX/CameraTransitions", os.path.join(ks, "lowFrequency_explosion_000.ogg"), "kenney_sci", "SFX", False, -16, "lowFrequency_explosion_000"),
    ("SFX_Magic_Settle", "SFX/CameraTransitions", os.path.join(ki, "glass_005.ogg"), "kenney_ui", "SFX", False, -19, "glass_005"),
    # experiment
    ("SFX_Voltage_Step", "SFX/Experiment", os.path.join(ki, "drop_002.ogg"), "kenney_ui", "SFX", False, -18, "drop_002"),
    ("SFX_Record", "SFX/Experiment", os.path.join(kd, "twoTone2.ogg"), "kenney_dig", "SFX", False, -18, "twoTone2"),
    ("SFX_Plot", "SFX/Experiment", os.path.join(kd, "pepSound2.ogg"), "kenney_dig", "SFX", False, -19, "pepSound2"),
    ("SFX_Part_Select", "SFX/Experiment", os.path.join(ki, "select_003.ogg"), "kenney_ui", "SFX", False, -18, "select_003"),
    ("SFX_Part_Place", "SFX/Experiment", os.path.join(ki, "drop_003.ogg"), "kenney_ui", "SFX", False, -17, "drop_003"),
    ("SFX_Key_Switch", "SFX/Experiment", os.path.join(ki, "switch_004.ogg"), "kenney_ui", "SFX", False, -17, "switch_004"),
    ("SFX_Condition_Reached", "SFX/Experiment", os.path.join(ki, "confirmation_003.ogg"), "kenney_ui", "SFX", False, -18, "confirmation_003"),
    ("SFX_Info_Chime", "SFX/Experiment", os.path.join(kd, "threeTone2.ogg"), "kenney_dig", "SFX", False, -19, "threeTone2"),
    ("SFX_Fact_Chime", "SFX/Experiment", os.path.join(ki, "glass_004.ogg"), "kenney_ui", "SFX", False, -19, "glass_004"),
]


def level(path, seconds):
    if seconds >= 3:
        r = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", path, "-af", "ebur128", "-f", "null", "-"], capture_output=True, text=True).stderr
        m = re.findall(r"I:\s+(-?[\d.]+) LUFS", r)
        if m and float(m[-1]) > -60:
            return float(m[-1]), "LUFS"
    r = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", path, "-af", "volumedetect", "-f", "null", "-"], capture_output=True, text=True).stderr
    m = re.findall(r"mean_volume: (-?[\d.]+) dB", r)
    return (float(m[-1]) if m else -20.0), "dBmean"


def duration(path):
    return float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True).stdout.strip() or 0)


entries, manifest = [], []
import glob
for name, cat, src, key, group, loop, ref, title in PICKS:
    if not os.path.exists(src) and "jingles_" in src:
        found = glob.glob(os.path.join(kj, "**", os.path.basename(src)), recursive=True)
        if found: src = found[0]
    if not os.path.exists(src):
        print("MISSING", name, src); continue
    ext = os.path.splitext(src)[1].lower()
    dst_dir = os.path.join(root, *cat.split("/"))
    os.makedirs(dst_dir, exist_ok=True)
    dst = os.path.join(dst_dir, name + ext)
    shutil.copyfile(src, dst)
    secs = duration(dst)
    lvl, unit = level(dst, secs)
    gain = round(min(1.0, 10 ** ((ref - lvl) / 20)), 3)
    author, lic, url = SRC[key]
    entries.append({"name": name, "path": dst.replace("\\", "/"), "group": group, "loop": loop, "gain": gain,
                    "seconds": round(secs, 2), "measured": lvl, "unit": unit, "reference": ref,
                    "source": title, "author": author, "license": lic, "url": url, "originalFile": os.path.basename(src)})
    manifest.append((name, cat, title, os.path.basename(src), author, lic, url, dst.replace("\\", "/"), round(secs, 1), lvl, gain))

# licences
lic_dir = os.path.join(root, "Licenses"); os.makedirs(lic_dir, exist_ok=True)
for pack in ["interface-sounds", "digital-audio", "sci-fi-sounds", "music-jingles"]:
    shutil.copyfile(os.path.join(K, "kenney_" + pack, "License.txt"), os.path.join(lic_dir, f"Kenney_{pack}_License.txt"))
open(os.path.join(lic_dir, "OpenGameArt_CC0_sources.txt"), "w", encoding="utf-8").write(
    "Licensed CC0 1.0 (https://creativecommons.org/publicdomain/zero/1.0/), as stated on each OpenGameArt page on 2026-10-14:\n"
    "- Intro Music by RonyDkid: https://opengameart.org/content/intro-music-0 (game_2.mp3)\n"
    "- Magic Puzzle In-Game 1 by MintoDog: https://opengameart.org/content/magic-puzzle-in-game-1 (magic_puzzle_in-game_1_bpm110.ogg)\n"
    "- Dark Sci-Fi Audio Pack by SRG774: https://opengameart.org/content/dark-sci-fi-audio-pack (airy_0, pulse_0, sector_0, victory_5, hover_0 .ogg)\n"
    "- Claimed by the Void by vitalezzz: https://opengameart.org/content/claimed-by-the-void (claimed_by_the_void_loop.mp3)\n")
open(os.path.join(lic_dir, "Mixkit_Sound_Effects_License.txt"), "w", encoding="utf-8").write(
    "Mixkit Sound Effects Free License (https://mixkit.co/license/), applies to the Mixkit sound effects in this folder\n"
    "(not Mixkit stock music, which was not downloaded). Items: Small electric glitch 2595, Electric buzz glitch 2596,\n"
    "Electric whoosh 2600, Electronics power up 2599, Electricity static power up 2602, Electric charge hum 332, Static\n"
    "electric crackles 2597, Futuristic electricity sparks 2138, Intense electricity hum 2593, Low electricity humming 2592,\n"
    "Magic sparkle whoosh 2350, Fairy magic sparkle 2638, Magic wand sparkle 2352, Magical light moving 3082, Magic transition\n"
    "sweep presentation 869, Magic sparkle poof hit 2407, Fantasy game success notification 2633, Cinematic whoosh fast\n"
    "transition 1492, Cinematic tunnel reverb woosh 2608, Fast sci fi transition sweep 2408, Technology alert transition 3111.\n"
    "Downloaded 2026-10-14 from https://assets.mixkit.co/active_storage/sfx/<id>/<id>.wav. Read the licence page for the\n"
    "current terms before redistributing the raw files; the licence permits use inside projects such as games but not\n"
    "redistribution of the sound files on their own.\n")

cue = {"entries": entries,
       "aliases": {  # old synthesized bank name -> library clip (old names keep working in untouched Timelines)
           "PadIntro": "BGM_Professor_Ohm_Laboratory", "PadLab": "BGM_Professor_Ohm_Laboratory", "PadLabTheme": "BGM_Wattson_Magical_Discovery",
           "PadWire": "BGM_Conductor_Airy", "WireHum": "LOOP_Electricity_Hum_Low", "CircuitHum": "LOOP_Electric_Charge_Hum",
           "Select": "SFX_Part_Select", "Place": "SFX_Part_Place", "Switch": "SFX_Key_Switch", "VoltStep": "SFX_Voltage_Step",
           "Record": "SFX_Record", "Plot": "SFX_Plot", "Highlight": "SFX_Fact_Chime", "Reveal": "SFX_Scientific_Discovery",
           "Rise": "SFX_Magic_Sweep", "Omega": "SFX_Discovery_Victory", "Finale": "SFX_Final_Success", "Transform": "SFX_Magic_Poof",
           "LabSweep": "SFX_Magic_Sweep", "Enter": "SFX_Tunnel_Woosh", "Exit": "SFX_Whoosh_Fast", "Count": "UI_Hover",
           "ui.hover": "UI_Hover", "ui.click": "UI_Click", "ui.open": "UI_Open", "ui.close": "UI_Close", "ui.done": "SFX_Correct_Answer",
           "ui.hint": "UI_Question", "ui.stamp": "UI_Stamp", "ui.wrong": "UI_Error", "ui.start": "UI_Confirm"},
       "cueRules": []}
cd = os.path.join(root, "CueDefinitions"); os.makedirs(cd, exist_ok=True)
json.dump(cue, open(os.path.join(cd, "library.json"), "w", encoding="utf-8"), indent=1)

doc = os.path.join(root, "Documentation"); os.makedirs(doc, exist_ok=True)
with open(os.path.join(doc, "Ohms_Audio_Asset_Manifest.md"), "w", encoding="utf-8") as f:
    f.write("# Ohm's Law audio asset manifest (downloaded 2026-10-14)\n\n")
    f.write("Sources: OpenGameArt (CC0 as stated on each page), Kenney (CC0, licence files in `Licenses/`), Mixkit sound effects "
            "(Mixkit Sound Effects Free License, see `Licenses/Mixkit_Sound_Effects_License.txt`; no Mixkit stock music was used). "
            "Original archive/file names are kept in the table; the Unity names are import names.\n\n")
    f.write("| Unity clip | Folder | Original title / file | Author | Licence | URL | Length | Level | Gain |\n|---|---|---|---|---|---|---|---|---|\n")
    for n, cat, title, orig, author, lic, url, dst, secs, lvl, gain in manifest:
        f.write(f"| `{n}` | `{cat}` | {title} (`{orig}`) | {author} | {lic} | {url} | {secs} s | {lvl} | {gain} |\n")
    f.write("\nNot imported (downloaded, auditioned by length/level only): `game_1.mp3` (identical to `game_2.mp3`), "
            "`claimed_by_the_void_loop.wav` (51 MB; the mp3 is used), the rest of the Kenney packs and the other Mixkit listings.\n")
print(f"{len(entries)} clips -> {root}; manifest + licences written")

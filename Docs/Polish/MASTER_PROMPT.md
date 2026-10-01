# POLISH PROMPTS: Ohm's Law Module, Scenes 5–13 (visual pass)

> **Purpose:** turn the playable blockout into the finished-looking module: Scene 4's cinematic language (lighting, post-processing,
> camera motion), real models, correctly sized text, real effects — **without changing a single line of dialogue, any interaction,
> any gate or any timing.** The flow stays exactly as reviewed; only what is on screen changes.
>
> **How this is used:** PROMPT P0 builds the shared look once. PROMPT P05 → P13 re-dress one scene each. PROMPT P-FLOW rebuilds
> everything, re-runs the hands-free dry run with a screenshot of every beat, reviews the screenshots against this document, and
> fixes what fails. Each scene prompt ends with acceptance criteria that are checked on the screenshots.

---

## What the blockout got wrong (from the research pass — these are the root causes, all fixed by P0)

| Symptom you saw | Cause | Fix |
|---|---|---|
| Giant white "Kabir / Manav / Ishaan / Meera" letters and grey eggs in every bench shot | kid stand-in capsules placed 1.2 m in front of the camera, name labels 0.25 m tall | the kids are the camera's point of view; no stand-ins (no student models exist) |
| Labels 2–3× too big, overlapping, cut off | sizes chosen in metres without converting for camera distance; every prop got a label; labels face world −Z, not the camera | one typography scale (cap height in metres per shot distance), only the storyboard's own on-screen text, every card faces its camera |
| Inside the wire looks like "space", grey wall in Scene 12 | tunnel mesh wound inward **and** front-face culled → the wall never renders | one visible metal tube with rib rings, fog and coloured lights |
| Flat grey everything, no mood | post-processing off, flat ambient, grey greybox materials | Scene 4's lighting rig + ACES/bloom/vignette volume; three looks (study, bench, wire) |
| Boxes instead of instruments, a cube lab | greybox stand-ins | Scene 4's real study prefab, the imported ammeter/voltmeter/source/switch/nichrome-wire models, Scene 4's Wattson with the real Ohm-niscope |
| Static cameras, hard cuts | one static vcam per beat | push-ins, dollies and orbits via two-camera blends, depth of field per shot, 1 s eases |
| Magenta "[VFX]" boxes | reviewer placeholders | real particle/line effects with Scene 4's recipes |

---

## PROMPT P0 — The look (shared, built once)

### 0. One world
- **Scenes 7–13 happen in Scene 4's study**, the same prefab at the same coordinates (floor y −1.08, desk top y −0.26, Ohm at his
  desk against the −X wall, the window light, the fireplace, the oil lamp). Scene 7 is literally Scene 4's room with Wattson and the
  kids' POV where Scene 4 left them; in Scene 8 the Victorian desk becomes the modern bench (2.0 × 0.9 m, same height) and from
  Scene 9 on everything happens on that bench, still inside the historical room ("the lab around stays historical").
- **Scenes 5, 6 and 12 happen inside the wire**: a 16 m tube of radius 2.5 m, copper-dark in 5–6, dull nichrome grey with a visible
  ion lattice in 12.
- The kids (Kabir, Manav, Ishaan, Meera) are the camera. Wattson addresses the lens.

### 1. Lighting and post-processing (three looks)
| Look | Scenes | Light | Post |
|---|---|---|---|
| **Study** | 7, 8 | Scene 4 rig: cool window directional (soft shadows), warm flickering oil lamp / desk candle / fireplace / bookcase candle, warm fill from the kids' side, baked reflection probe, god rays through the window, exponential warm fog | ACES, bloom 0.85 (threshold 1), vignette 0.26, film grain 0.22, exposure +0.25, contrast 8, saturation 6 |
| **Bench** | 9–13 | the same room, plus a cool white bench light over the instruments (so dials and digits read) and a soft amber key on Wattson | same volume, bloom 0.9 so the current path and slot glows bloom |
| **Wire** | 5, 6, 12 | no sun; three coloured point lights down the tube (charge-cyan, tint, voltage-blue), fog in the wall tint, carriers self-lit (HDR, bloom) | ACES, bloom 1.2, vignette 0.32, exposure +0.15; Scene 12 10 % darker and desaturated so the resistance beat feels heavier |

Depth of field on every shot (Bokeh), focus following the shot's subject: close-ups f/1.8 (70 mm), mediums f/3.2 (45 mm),
establishing f/5.6 (28 mm); the Scene 12 dive adds motion blur + chromatic aberration like Scene 4's dive.

### 2. Cameras (motion, not cuts)
- Cinemachine, brain default = cut; **every move is a blend between two virtual cameras** (A at the start pose, B at the end pose,
  overlapping clips) — that is how Scene 4 does its push-ins, dollies and orbits. Rules: one move per beat; close-ups FOV 30–36;
  mediums 38–45; establishing 46–50 with mild handheld noise; dolly-zoom 22 → 82 for dives.
- Each scene's shot list is in its prompt below. Match-cut pairs (5→6, 7→8→9→10→11→12→13) start on the previous scene's last pose.
- Wattson is never on the line between camera and subject; he hovers 2–3 m from the lens in the lower-left or lower-right third.

### 3. Text (one typography scale)
World text is 3D TextMeshPro in the module font (Nunito Bold, DejaVu fallback for Ω ∝ Δ → ÷ × −). Sizes are **cap heights chosen for
the camera that reads them** (formula: fontSize = cap_m × 14.06):

| Tier | Screen cap at 1080p | Used for | Cap height |
|---|---|---|---|
| Caption | 24 px | meter captions "Measures current — I — ampere (A)", slider ticks | 0.025 m (bench close-up) |
| Label | 28 px | the storyboard's prop names (AMMETER, NICHROME WIRE — XY, …), path label | 0.03–0.045 m |
| Badge | 36 px | X / Y terminal cards, meter digits, "Symbol: V / Unit: volt (V)" sub-lines | 0.04 m bench · 0.13 m wire |
| Data card | 56 px | V / I = CONSTANT, RESISTANCE — R, R = V / I, V × 2 → I × 2, Slope = ΔV / ΔI, 1 Ω = 1 V / 1 A, V ∝ I, Voltage → V | 0.15 m at 3.5 m |
| Concept word | 64 px (Scene 4's VOLTAGE/CURRENT size) | VOLTAGE, CURRENT, RESISTANCE | 0.23 m in the wire |
| Hero | 100–115 px | V = IR, Ω, the V / I / R letters | 0.26–0.3 m |
| Title | 120 px | OHM'S LAW, WHAT MAKES CHARGE FLOW? | 0.32–0.4 m |
| Prose card | 44 px | "Is there a mathematical relationship…?", the Scene 9 reminder | 0.11 m, wrap 2.7 m, max 3 lines |

Two card styles: **glow words** (Scene 4's recipe: HDR face in the concept colour, outer glow, pop-in with overshoot, slow bob, spark
burst on appear — no backing) for concept/hero/title words; **panel cards** (rounded dark panel, 0.8/0.5-cap padding) for prose,
data and badges. Every card faces the camera that reads it. **Only the storyboard's on-screen text exists** — no debug labels on
props, no object names. Screen HUD: subtitles 44 px body in a ≥ 95 %-opaque box (long lines page correctly), MCQ 40/36 px on a
rounded panel that never covers the thing it asks about, prompt 42 px pill, 88 px minimum touch targets.

### 4. Real models
| Thing | Asset | Notes |
|---|---|---|
| Wattson | `Scene04/Prefabs/Wattson_Scene04.prefab` | scale 0.34, hover bob, glow, spark trail, follow light, real Ohm-niscope (rings open, lens flash, beam) |
| Georg Ohm | `Scene04/Prefabs/Ohm_Scene04.prefab` | scale 1.55, work loop (write / check galvanometer / adjust wire); freeze = hold pose, never T-pose |
| Study | `Scene04/Prefabs/Lab_Scene04.prefab` | lights off, flames emissive, Scene 4's dressing |
| 1826 apparatus | Scene 4's copper wire on brass posts, galvanometer, voltaic pile, notebook + **new:** Cu–Bi thermoelectric source (hot / cold junctions), 4 copper test wires, 4 mercury cups, handwritten pages | Scene 4 materials (copper, brass, zinc, dark wood, paper, ink) |
| Modern bench | `ImportedLabEquipment/Workbench.prefab` scaled to 2.0 × 0.82 × 0.9 m at the desk's place | or desk material swap; the room stays |
| Ammeter / Voltmeter | `ImportedLabEquipment/Ammeter.prefab`, `Voltmeter.prefab` at 0.22 | real dials face the camera; needles driven from the rest pose; voltmeter dial repainted 0–6 so 5 V is honest; digit readouts as badges |
| Nichrome wire XY | `ImportedLabEquipment/NichromeWireXY.prefab` at 0.5 (1.0 m) | dull metal, X/Y badges at the connectors, **never glows** |
| DC source | `ImportedLabEquipment/DCSource.prefab` at 0.19 | knob turns with the voltage, pilot lamp lights when the key is closed |
| Key | `ImportedLabEquipment/Switch.prefab` (or the lever switch) | moving part animates open/closed, tap collider |
| Cables | insulated leads with sag along Source → Ammeter → X, Y → Key → Source; voltmeter leads X / Y → voltmeter | the series path lights amber when the ammeter is placed |
| Charge carriers | ~700 glowing spheres with the "−" glyph | HDR so bloom lights them |
| Table / graph | world-space boards, module font, palette colours, legible from the boards camera (≥ 20 px) | table left-low, graph right-high, never overlapping |

### 5. Effects (replacing every magenta tag)
field wave (one ring of light sweeping − → + along the whole tunnel in 0.4 s), time-freeze (cool tint pulse + sparkle, carriers hold),
Ohm-niscope flash (Scene 4's lens flash + white flare), letters assembling from charges (particles converge into the title card),
race into the wire surface (Scene 4's dive streaks + beam), galvanometer needle swing (the needle actually swings), apparatus
transformation (old prop scales down with sparks while the new one scales up), X and Y pulse (badge pulse + glow ring), slot glows
(soft pulsing additive discs), snap flash, key click spark, graph pulse, carriers impeded (bounce + dim), RESISTANCE forming from the
lattice (ion sparks converge), title reveal (letters ignite with sparks), columns-become-axes (the table's I column lays down as the
x-axis, the V column rises as the y-axis).

### P0 acceptance
Project compiles; a rebuilt Scene 7 shows Scene 4's room, lit like Scene 4, with Ohm working and no stand-ins; a rebuilt Scene 9
shows the real instruments on the bench with readable 28 px labels; a rebuilt Scene 5 shows a visible copper tunnel full of glowing
carriers with the cards fully in frame.

---

## PROMPT P05 — Scene 5 "Journey Inside the Wire"
**Set & look:** Wire look, copper tint. First 3 s: Scene 4's actual desk and copper wire (same prefab, same coordinates) with the
Ohm-niscope beam on it — the literal continuation of Scene 4's last frame.
**Shots:** (1) macro on the copper wire, 22 → 82 dolly-zoom with dive streaks and a flash to white; (2) inside: slow push along the
tunnel axis from the platform (FOV 60 → 52) while carriers jitter with no drift; (3) hold on Wattson on the platform for L01–L03, he
points at a passing carrier; (4) wide on the WHAT MAKES CHARGE FLOW? title (centre, Title tier) assembling from charges, then a slow
orbit around it as the two terminal badges light at the tunnel ends — both in frame, − left, + right, neutral battery colours;
(5) Wattson raises the Ohm-niscope: the field wave sweeps the whole tube once, every carrier starts drifting − → + together (never a
push from one end); (6) L04–L05 on the glowing bracket between two lit points at eye level, the VOLTAGE glow word with its two badge
sub-lines on the left at 3.5 m; (7) the cross-section ring slides into frame, carriers flash through it, the counter with its
one-second ring beside it, L06–L07, CURRENT word on the right at the same depth; (8) L08 two-shot: Wattson lower-left, VOLTAGE and
CURRENT floating left and right; (9) L09 Wattson turns to the lens, Ask Wattson pulses once.
**Continuity out:** camera pose, carriers drifting, both cards, counter and ring all persist into Scene 6.
**Accept:** tunnel wall visible in every frame; every card fully inside the frame; carriers never still; − → + drift only after the wave.

## PROMPT P06 — Scene 6 "Adaptive Question"
**Set & look:** identical to Scene 5's last frame. **Shots:** (1) Wattson turns to the lens for L01; (2) slow push on the + end as
the battery readout rises and the time-freeze pulse holds every carrier (jitter only); (3) L02 and the prediction MCQ docked to the
right, the tunnel still visible left; (4) the reaction line and L03 on Wattson; (5) he raises the Ohm-niscope toward the wall, rings
open, lens flash, flare to white. **Accept:** no visible change of drift speed at any point; MCQ never covers Wattson.

## PROMPT P07 — Scene 7 "Back to Georg Ohm"
**Set & look:** Study look. Scene 4's room, Ohm at his desk in his work loop, the apparatus plus the thermoelectric source, test
wires, mercury cups, notebook. **Shots:** (1) fade in from white on Scene 4's "arrival" framing, slow settle; (2) Wattson at Scene 4's
hover point for L01, the two data cards "Voltage → V" / "Current → I" pop in either side of him (Data tier); (3) L02–L04 over Wattson's
shoulder to Ohm, rack focus to Ohm on L03, the prose question card appears above the desk on L04; (4) desk close-up for L05–L07: Ohm
places a wire, adjusts the source, the galvanometer needle swings, he writes — one notebook page per "again", pages fan out on L07;
(5) L08–L09 medium on Wattson, the first flicker of transformation on the apparatus (sparks, a brief scale pulse).
**Continuity out:** the desk camera pose and all desk props carry into Scene 8. **Accept:** room lit like Scene 4; no labels except
the storyboard's; Ohm never T-poses.

## PROMPT P08 — Scene 8 "From Ohm's Lab to Our Lab"
**Shots:** (1) match-cut; Wattson freezes Ohm with the Ohm-niscope (Ohm holds his pose, a cool tint flash); L01; (2) slow push on the
desk while, one item every 0.8 s, each old prop scales down in sparks and its modern replacement scales up with its label badge:
galvanometer → AMMETER, VOLTMETER appears, copper wires → NICHROME WIRE — XY with X and Y lighting, source, key, and finally the
wooden desk becomes the clean bench; (3) L02 on Wattson beside the bench, the ammeter and voltmeter waiting in the parts tray.
**Accept:** every label at Label/Caption tier, nothing overlapping; the room around stays historical; Ohm still visible, frozen.

## PROMPT P09 — Scene 9 "Build the Circuit"
**Shots:** (1) Wattson flies to the wire (off the lens axis), the wire highlights with Scene 4's outline shimmer, L01–L04 on a slow
push to the wire; (2) Wattson at the tray, L05, the series slot pulses — ammeter, slot and the learner's hand path all inside the
frame; (3) on snap: flash, the amber current path lights along the cables with the path label above; L06; (4) Wattson to the
voltmeter, L07, X and Y badges pulse, the parallel slot glows behind the wire (never in front of it); (5) L08; (6) L09 with the prompt
pill and the tap ring on the key; on tap: click spark, the key closes, needles move (0.5 V), the pilot lamp lights; (7) L10–L11 medium
on Wattson with the reminder prose card above the bench; L12.
**Accept:** every interaction target visible when its gate opens; the nichrome wire never glows; the reminder card fully in frame.

## PROMPT P10 — Scene 10 "Become the Experimenter"
**Shots:** (1) match-cut; the BATTERY control slides in bottom-left; L01; (2) bench close-up on the voltmeter for L02 (1.0 V on the
dial and the digit badge), on the ammeter for L03 (0.20 A) — the RECORD button beside it, never over a reading; (3) on record the
row flies into the table (left board); L04; (4) boards camera: the knob turns 2 → 5 V, needles and digits update, rows fill; (5)
L05–L07 on the table; the pattern MCQ docks to the right half with the table visible on the left; (6) L08–L10 rows 1–2 highlight with
their tags; (7) the V × 2 → I × 2 card grows out of those two rows; L11.
**Accept:** readings legible (≥ 20 px) in every table shot; voltage continuity 0.5 → 1 → 2…5 V with no resets.

## PROMPT P11 — Scene 11 "Turn Data into a Graph"
**Shots:** (1) boards camera; the table's I column lays down as the x-axis and the V column rises as the y-axis (the real effect);
(2) points plot one by one with their coordinates, the voltage-blue line draws through the origin, bloom; L01; (3) slow push on points
1–2 as they highlight with I → 2I / V → 2V, graph pulse; L02–L03; (4) L04 wide on both boards, V ∝ I glow word at the end of the line
inside the board; L05 on Wattson beside the graph.
**Accept:** axes titles and ticks legible; line and points high-contrast; nothing clipped.

## PROMPT P12 — Scene 12 "Discover Resistance"
**Shots:** (1) Wattson aims the Ohm-niscope at the nichrome wire; dolly-zoom into the rod with beam and dive streaks, flash; (2)
inside: nichrome-grey tube, carriers drifting, the ion lattice fading in; L01 VOLTAGE word left, L02 CURRENT word right, L03;
(3) carriers slow and bounce off ions, the tube narrows ≤ 20 % (metaphor toggle); L04; (4) ion sparks converge into the RESISTANCE
glow word, Symbol: R badge; L05–L06; (5) cross-fade to the boards camera: table (now with units) and graph side by side; L07 the V / I
column appears, three worked divisions then two quick fives, the column glows; L08–L10 the data cards morph CONSTANT → RESISTANCE — R
→ R = V / I beside the table; (6) graph close-up: the ΔV/ΔI triangle draws with its values; L11–L12; the slope MCQ docks right; (7)
L13–L15: the big Ω hero glyph (emissive, same glow as the words) and the 1 Ω = 1 V / 1 A card.
**Accept:** the wall renders at every radius; lattice = soft spheres not squares; Ω glows; every card inside the board frame.

## PROMPT P13 — Scene 13 "The Law Gets Its Name"
**Shots:** (1) match-cut on the boards, which dim as (2) three hero letter cards rise above the bench, each letter with its name as a
smaller second line (V blue · I amber · R coral), spaced so none overlap; L01, each card pulses on L02–L04; (3) they slide together into
V = IR (hero) on L05; (4) L06: OHM'S LAW title ignites letter by letter with sparks above the formula; (5) the checkpoint MCQs dock
right with the title dimmed; (6) L07 on Wattson, white-out to the Scene 14 card.
**Accept:** no card leaves the frame; the wall behind the cards is the real room, not void.

---

## PROMPT P-FLOW — Rebuild, dry run, review, fix
1. Rebuild Scenes 5–13 (VO unchanged, so timings hold). Compile with zero errors.
2. Hands-free dry run 4 → 13 with a screenshot of every beat (`Logs/Blockout/shots/`).
3. Review every screenshot against the acceptance lines above; list failures per scene; fix; rebuild; re-run.
4. Regenerate `Docs/FLOW.md` and `Docs/BLOCKOUT_REPORT.md` (remaining placeholders should be the few things the storyboard leaves
   open, not visuals).
5. Final report: per-scene shot list as built, what is real vs still placeholder, and the toggles you can flip.

### Acceptance (whole pass)
- [ ] Every frame of the dry run is lit and post-processed; no greybox primitive, no magenta tag, no stand-in capsule is visible.
- [ ] All on-screen text is the storyboard's text, at the typography tier for its shot, fully inside the frame, facing its camera.
- [ ] Every beat has camera motion or a deliberate hold; match-cuts are seamless (same pose, same visible objects, same readings).
- [ ] Real instruments, real room, Scene 4's Wattson and Ohm everywhere they appear.
- [ ] Dialogue, subtitles, interactions, gates, timings and hand-offs are byte-identical to the blockout's.

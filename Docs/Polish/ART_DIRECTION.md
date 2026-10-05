# Art direction — Scenes 5–13 match the uploaded references

The nine reference images in `Docs/Polish/References/` are the FINAL VISUAL TARGET (the user's master prompt, `Docs/Polish/USER_MASTER_PROMPT.md`).
This file turns them into buildable specs. Content rules still win where a reference would change the lesson (see §9).

## 1. Reference → scene map
| File | Scene / beat | What it shows |
|---|---|---|
| `Scene05_terminals_electrons_L04.png` | 5, S05_L04 | dark-blue electron space with a reflective floor; − terminal (blue/cyan, left) and + terminal (red, right) as big metal blocks; a translucent conduit between them; glowing cyan electrons with "−" in several depth layers; Wattson small, standing centre-left, pointing |
| `Scene06_concept_panels_L02.png` | 6, S06_L02 | same space; two floating concept panels (dark blue, cyan edge, orange title, white body, small cyan diagram); Wattson centre pointing up |
| `Scene07_two_clues_L01.png` | 7, S07_L01 | bright stylised classroom: dark-green board with chalk text, blue cabinets, posters, plants; Ohm behind a wooden desk with X-wire-Y and copper wires; Wattson flying at left; blurred student heads at the bottom |
| `Scene07_board_question_L04.png` | 7, S07_L04 | same room; board "Current → I / Voltage → V" with definitions and a simple circuit diagram; Ohm gesturing at the board; apparatus on the desk |
| `Scene07_galvanometer_closeup_L05.png` | 7, S07_L05 | hero close-up of the desk: torsion galvanometer in a glass cylinder, mercury cups, copper test wires, notebook, source; callout panels; compact magenta "[VFX] galvanometer needle swing" panel top-left |
| `Scene07_ohm_copper_wires_L08.png` | 7, S07_L08 | Ohm (period dress) examining a copper wire at the desk; board behind; audience silhouettes |
| `Scene08_modern_apparatus_presented.png` | 8, after the swap | Ohm presenting (arms open) the modern apparatus: DC source, ammeter, X–nichrome–Y, switch, red cables; callouts; a header panel "Investigating Electric Circuits / Controllable DC Source" top-left |
| `Scene09_series_ammeter_L06.png` | 9, S09_L06 | bench close-up of the circuit with callouts, no Ohm; bright lab light |
| `Scene09_pulse_XY_callouts.png` | 9, ammeter snap / path lights | magenta pulse travelling X → Y along the nichrome wire with chevrons; "[VFX] Pulse travels from X to Y" panel |
| (no reference) | 10, 11, 12, 13 | extend the same language: classroom + bench, boards for the data (table/graph as teaching boards), concept panels, callouts |

## 2. The room (Scenes 7–13) — "polished stylised classroom lab"
Replaces the Victorian study for these scenes (Scene 4 keeps its study; Scene 5 opens on Scene 4's wire for 3 s).
- Frame: desk centre at the origin, desk top y = 0.80, desk 2.0 × 0.9 m, Ohm stands behind it (+Z side) facing the camera; cameras on the −Z side (so world text faces −Z and reads).
- Back wall at z ≈ +2.6: pale blue-grey wall, a dark-green teaching board (3.2 × 1.5 m, wooden frame) centred behind the desk at eye height 1.6 m; blue/navy base cabinets along the wall (0.9 m high) with a wooden worktop; two posters in frames ("CIRCUITS" with a circuit sketch, "THINK / EXPLORE · QUESTION · LEARN" with a bulb), a window with daylight at the left, a potted plant, stacked books, a globe.
- Palette: wall `#9FB3C8`-ish, cabinets navy `#3B4F7A`, board `#1E3A34`, desk oak `#7A4B2B` (Scene 7) / clean light bench `#E6E8EC` (8+), accents orange `#FFB830`, cyan `#BFF6FF`.
- Lighting: warm key from the window (directional, soft shadows), cool fill from the ceiling, a soft rim on characters; baked-style ambient (Trilight: sky light-blue, equator warm, ground dark); post: ACES, bloom 0.6 threshold 1.1, vignette 0.2, exposure +0.2, subtle grain, DoF per shot (Bokeh) with the background softly out of focus.
- Audience: three or four blurred dark student heads (spheres/capsules, navy, matte) at the bottom edge, 0.6–1.0 m from the lens, strongly defocused — toggle `audience`, on for Scenes 7 and 8, off when the learner must see the bench top (9–13) unless the shot is wide.

## 3. The teaching board (`Board` helper)
A dark-green plane with chalk-style TMP text (slightly off-white, 0.07–0.12 m caps) and line-renderer diagrams. Per scene content comes from the storyboard's on-screen text:
- 7: left "Voltage → V" (blue), right "Current → I" (amber); after L04 the question card "Is there a mathematical relationship between voltage and current?" as a chalk panel; a simple battery–wire loop diagram.
- 8: "Our instruments today" line with AMMETER / VOLTMETER / NICHROME WIRE — XY as chalk items appearing with the swap.
- 9: the current-path label "Source → Ammeter → Nichrome Wire XY → Switch → Source" appears on the board when the ammeter snaps; the reminder "If potential difference increases, what happens to current?" at L10–L11.
- 10: the observation table IS the board (table headers "Potential Difference V (V)" | "Current I (A)").
- 11: the V–I graph on the board, axes "Current, I (A)" / "Potential Difference, V (V)", points, line, "V ∝ I".
- 12: table + V/I column + "R = V / I", "Slope = ΔV / ΔI", "1 Ω = 1 V / 1 A"; 13: "V = IR", "OHM'S LAW".
Nothing on the board may state a result before its scene (see §9).

## 4. Callout system (screen-space, reusable)
`ObjectCallout`: a canvas panel (dark navy `#1B2233` at 0.95, 2 px orange border, 14 px radius) with an orange title (40 px) and a white description (30 px, ≤ 2 lines, panel ≤ 580 px wide), a 2 px orange leader line to a 12 px orange marker dot on the world target, positioned at a configurable screen anchor (top-left / top-centre / top-right / bottom …) so panels never overlap each other or the HUD/subtitles. Driven by cues `callout show <id>` / `callout hide <id>`; builders declare them with `Callout(id, targetTransform, title, description, anchor)`. Titles/descriptions use the storyboard's label text where it exists:
- AMMETER — "Measures current — I — ampere (A)"; VOLTMETER — "Measures potential difference — V — volt (V)"; NICHROME WIRE — XY — "Test conductor between X and Y"; cells / controllable DC source — "Provides a controllable potential difference"; switch / key — "Completes or breaks the circuit"; torsion galvanometer — "A needle twists when current flows"; mercury cups — "Connection points for the wires"; copper test wires — "Wires of different lengths"; notebook — "Ohm's handwritten measurements"; copper–bismuth thermoelectric source — "Provides a steady potential difference".
`VfxPanel`: compact magenta-outlined panel (dark fill, magenta accent icon, white text, "[VFX]" in magenta) at top-left or top-centre, text 32 px, ≤ 560 px wide (about 29 % of a 1920 px frame), shown while the effect plays. Keep the existing "[VFX] …" names so the cues work.
X / Y badges in the world: small dark cubes with an orange letter on the near face at each wire terminal (reference), 0.04 m cap.

## 5. The bench and instruments (8–13)
Reference layout, left → right on the 2.0 m top: DC source (left, front panel to camera, red + / black − posts), red cable to the ammeter (centre-back, dial to camera, 0–3 A), cable to X post (black block + metal post + "X" badge), the nichrome wire (straight, dull gold-grey, slightly raised on two posts) to Y, then the switch/key (right, lever) and the black return cable to the source. Voltmeter behind the wire on its own posts (Scene 9+). Cables: red for the positive side, black for the return, sag, 6 mm. Nothing floats: every object's base sits on the top with a soft contact shadow (AO decal quad). Instruments are the imported prefabs; the X/Y posts, badges and cable routing are procedural.

## 6. Pulse X → Y (Scene 9, on the ammeter snap and again when the key closes)
A magenta/pink glowing pulse (sphere + short trail, 3 chevrons ahead) travelling along the actual wire from X to Y in 1.2 s, emissive with a faint reflection on the bench; the wire itself does not change colour (it never glows); the "[VFX] Pulse travels from X to Y" panel is shown during it.

## 7. Electron space (Scenes 5, 6, 12)
Per the references: a dark-blue void with a reflective floor grid (Wattson stands on it, lower-left third, ≈ 0.7 m tall on screen), a translucent conduit (the "wire", radius ≈ 0.6 m) running left → right between a − terminal block (brushed metal, cyan-lit "−" disc) at the left and a + terminal block (red-lit "+" disc) at the right; electrons = glowing cyan spheres with a dark "−" bar in 3 depth layers (near large and blurred, mid, far small), ~150 inside the conduit + ~250 in the space, never covering Wattson or the subtitle band. Before the field: random motion everywhere; after: a single sweep of light along the conduit and all electrons drift − → +. Scene 12: the same space in nichrome grey-green with the ion lattice as dull spheres in the conduit.

## 8. Characters
- Ohm: the existing model (dark period coat, curly hair) — matches the period-dress references. Poses by OhmWorker: working loop (Scene 7), `Present` (arms open toward the apparatus, Scene 8), `Freeze` holds the pose. Never T-pose; never clipped by UI; face visible.
- Wattson: Scene 4's prefab; flying (bob + glow), left of the desk in classroom shots, lower-left in the electron space; never between camera and subject.

## 9. Content guardrails (where the references would change the lesson — keep the storyboard)
- Reference concept panels read "Greater potential difference pushes electrons harder" / "More electrons flow each second when voltage increases" — the second one answers the Scene 6 prediction. Use: **Voltage (V)** — "The electrical push between two points. Unit: volt (V)" and **Current (I)** — "Charge flowing past a point each second. Unit: ampere (A)".
- Reference boards show "Higher resistance (R) reduces current", "I ∝ V" and a V–I graph during Scene 7: resistance is Scene 12, proportionality is Scene 11. Scene 7's board shows only Voltage → V, Current → I, the question, and a circuit sketch.
- The reference ammeter dial is 0–3 A; our readings go to 1.0 A — fine. The voltmeter dial is repainted 0–6 V so 5 V reads honestly.
- Callout text is a label, not a lesson: no numbers or results that a later scene reveals.
- Every spoken line, subtitle, interaction, gate and timing stays exactly as built.

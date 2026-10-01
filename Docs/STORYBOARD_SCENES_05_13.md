# Ohm's Law module — storyboard for the Unity scenes 5–13

Source of truth for dialogue (word for word), on-screen text, beats, interactions and agent context.
Scenes 1–3 and 14+ are external videos. Scene 4 is already built (`Scene04_MeetWattson`).
Wattson is the AI detective companion; every line he says is voiced; students can "Ask Wattson".
Target: PC (mouse) and tablets (touch), landscape, Class 10 students.

## Accuracy rules (content, not polish)

- Inside a metal, the charge carriers are free electrons, shown as small glowing spheres with a "−" glyph.
- Before a battery is connected, electrons move randomly in all directions with no net drift. Never shown still.
- When the battery is connected, the electric field is set up along the whole conductor almost instantly, and all electrons start drifting together. Never show the battery pushing one electron into the next.
- Electrons drift from the − terminal end toward the + terminal end. Terminals are labelled. No conventional-current arrows in Scenes 5–6.
- The cross-section counter shows charge passing per second.
- The nichrome wire never glows or heats visually.
- All readings come from `ExperimentData`: V = {1,2,3,4,5} V, I = {0.20,0.40,0.60,0.80,1.00} A, R = 5. V on the vertical axis, I on the horizontal axis. Slope = ΔV/ΔI = 5. Ohms are introduced only in Scene 12.
- Resistance and collisions with the lattice appear only from Scene 12.
- Subtitles show text exactly as written (no markdown bold).
- Palette: Voltage `#3FA9FF`, Current `#FFB830`, Resistance `#FF5A5F`, Charge `#D6FAFF`, Wattson accent `#BFF6FF`.

## Pronunciation lexicon (TTS only)
Georg → "GAY-ork", Ohm-niscope → "OHM-nih-scope", nichrome → "NY-krome", XY → "X Y", V/I → "V divided by I", ΔV → "delta V", ΔI → "delta I", Ω → "ohm".

---

## Scene 4 (existing) — Meet Wattson
Lab (Georg Ohm's study), Ohm working at a bench (loop), Wattson flies in to the group.
S04_L01 "Welcome, detectives." (points toward Ohm) · S04_L02 "Your Christmas lights gave you a question. Nearly two centuries ago, Georg Ohm was investigating the relationship that can help us answer it." · floating words VOLTAGE and CURRENT · S04_L03 "He wanted to know how these two are connected." · S04_L04 "But before we understand his experiment..." (raises the Ohm-niscope) · S04_L05 "...you need to see what electricity looks like from the inside." (aims at a wire on Ohm's table) · S04_L06 "Everybody in." → flash to white → Scene 5.

---

## Scene 5 — Journey Inside the Wire (`Scene05_InsideTheWire`)
Enters from: Scene 4 ends with a flash to white as Wattson's Ohm-niscope beam hits a copper wire on Ohm's table. Exits to: Scene 6 (same place, match-cut, no fade).
Where: inside the copper wire (`WireInterior`, copper tint). Wattson + the four kids (Kabir, Manav, Ishaan, Meera) on a floating platform (kids = POV camera if models are missing).

Dialogue (exact):
| ID | Text | Fallback |
|---|---|---|
| S05_L01 | These are electric charges. | 2.2 s |
| S05_L02 | But seeing charges isn't enough. | 2.4 s |
| S05_L03 | The important question is... | 2.2 s |
| S05_L04 | The battery creates a potential difference — voltage — between two points. | 5.0 s |
| S05_L05 | That provides the electrical push that drives charge through the circuit. | 4.5 s |
| S05_L06 | And when electric charge flows... | 2.4 s |
| S05_L07 | ...we call that electric current. | 2.4 s |
| S05_L08 | And that's exactly the clue we need for your Christmas lights. The new battery changed the electrical conditions—but before we solve your mystery, we need to find out exactly how voltage and current are connected. | 12.0 s |
| S05_L09 | Want to predict what we'll find? | 2.2 s |

On-screen text (exact): `WHAT MAKES CHARGE FLOW?` · `VOLTAGE` / `Symbol: V` / `Unit: volt (V)` · `CURRENT` / `Symbol: I` / `Unit: ampere (A)` · battery terminals `+` and `−`.

Beats (~75 s):
1. [VISUAL] Fade in from white on a macro copper wire; camera pushes in; fade out. VFX: race into wire surface.
2. [VISUAL] Fade in inside the tunnel. Carriers in random motion, no drift. Platform with Wattson and kids glides in.
3. [VO] S05_L01 → S05_L02 → S05_L03.
4. [VISUAL] Giant `WHAT MAKES CHARGE FLOW?` card. VFX: letters assemble from charges. Hold 3 s.
5. [VISUAL] Card dissolves. Battery terminals appear at the two tunnel ends: `+` one end, `−` the other, labelled.
6. [VISUAL] Wattson "activates" (RaiseGadget). VFX: field wave sweeps the whole tunnel (0.3 s). All carriers begin drifting − → +, jitter kept.
7. [VO] S05_L04 (two points along the tunnel light up with a bracket between them) → S05_L05.
8. [VISUAL] `VOLTAGE` card (Voltage blue) with `Symbol: V` and `Unit: volt (V)`.
9. [VISUAL] Cross-section plane appears across the tunnel; carriers flash as they cross. [VO] S05_L06.
10. [VISUAL] Counter beside the plane ticks per crossing, with a 1-second ring that resets. [VO] S05_L07.
11. [VISUAL] `CURRENT` card (Current amber) with `Symbol: I` and `Unit: ampere (A)`.
12. [VO] S05_L08, with the VOLTAGE and CURRENT cards floating left and right.
13. [VO] S05_L09. Wattson turns to the camera. Ask Wattson button plays a one-time spotlight pulse.
14. [HANDOFF] Save `SceneHandoffState` (camera pose, fieldOn = true, drift speed, card positions). Signal `Scene05Complete` → Scene 6, no fade.

Interactions: none (Ask Wattson available throughout).
Agent context — Covered: everything from Scene 4; free electrons as charge carriers; random motion with no drift before a battery; a battery creates a potential difference (voltage) between two points; voltage is the push; V, volt; field along the whole circuit; charge crossing a section per second is current; I, ampere. Do not spoil: how V and I are related, V ∝ I, V = IR, R = V/I, resistance, ohms, the name "Ohm's law", the Christmas-lights answer. Chips: "Why were the charges moving before the battery?" · "Which way do electrons move?" · "What is one ampere?"

---

## Scene 6 — Adaptive Question (`Scene06_AdaptiveQuestion`)
Enters from: Scene 5, match-cut inside the wire. Exits to: Scene 7 (the lab), via the Ohm-niscope flash to white.

| ID | Text | Fallback |
|---|---|---|
| S06_L01 | Detective test. | 1.5 s |
| S06_L02 | If we increase the potential difference while everything else stays the same, what do you predict will happen to the current? | 6.5 s |
| S06_L03 | Interesting. Remember your answer. Let's see if Ohm's experiment agrees with you. | 5.0 s |

PROPOSED per-option reactions (must not reveal the answer): S06_R_A (after A. Increase) "A bold prediction, detective. Hold on to it." · S06_R_B (B. Decrease) "A surprising theory. Every good detective tests a theory." · S06_R_C (C. Stay the same) "A careful guess. Let's put it to the test."
Options (exact): `A. Increase` · `B. Decrease` · `C. Stay the same`

Beats (~30 s + wait):
1. Match-cut from Scene 5. Wattson turns toward the learner. [VO] S06_L01.
2. Wattson changes the battery voltage: the terminal readout/brightness rises. At the same moment Wattson freezes the carriers with the Ohm-niscope (VFX time-freeze) so the drift does not visibly speed up before the learner answers. Frozen (jitter only) until the scene ends.
3. [VO] S06_L02.
4. [INTERACTION] `S06_Prediction` MCQ with the three options → `LearnerSession.prediction` (A/B/C). No right/wrong feedback.
5. [VO] matching S06_R_* reaction, then S06_L03.
6. Wattson raises the Ohm-niscope and looks toward the tunnel wall. VFX Ohm-niscope flash. Fade to white.
7. [HANDOFF] `Scene06Complete` → Scene 7.

Agent context — Covered: same as Scene 5. Do not spoil: the answer to the prediction plus Scene 5's list. If asked "what's the right answer?", Wattson says a good detective tests it, and that the experiment is next. Chips: "What does 'everything else stays the same' mean?" · "What is potential difference again?" · "How did Ohm test things?"

---

## Scene 7 — Back to Georg Ohm (`Scene07_BackToOhm`)
Enters from: Scene 6, fade in from white. Exits to: Scene 8 (same lab, same camera; match-cut).
Where: the historical lab (Scene 4's study). Ohm at his table, working. Wattson and kids near the table.

| ID | Text | Fallback |
|---|---|---|
| S07_L01 | Now you know the two clues. | 2.2 s |
| S07_L02 | But Ohm wanted something more than definitions. | 3.0 s |
| S07_L03 | He wanted to know... | 1.8 s |
| S07_L04 | Is there a mathematical relationship between voltage and current? | 4.2 s |
| S07_L05 | In 1826, Ohm kept changing one electrical condition, the potential difference and observing what happened to the current. | 7.5 s |
| S07_L06 | Again. And again. And again. | 2.8 s |
| S07_L07 | He wasn't looking for one reading. He was looking for a pattern. | 4.5 s |
| S07_L08 | Today, we can investigate the same relationship much more clearly. | 4.2 s |
| S07_L09 | So let's run the investigation ourselves. | 2.8 s |

On-screen text (exact): `Voltage → V` · `Current → I` · big card: `Is there a mathematical relationship between voltage and current?`
Ohm's table props: `copper–bismuth thermoelectric source` (two junctions, one "hot", one "cold") · `torsion galvanometer` · `copper test wires` (3–4 lengths) · `mercury cups` (connection points; never operated by the learner) · notebook with accumulating `handwritten measurements` pages.

Beats (~60 s):
1. Fade in from white. Ohm works (loop). Wattson floats beside the group.
2. [VO] S07_L01. Cards `Voltage → V` and `Current → I` float beside Wattson.
3. [VO] S07_L02 → S07_L03 → S07_L04, big question card appears on S07_L04.
4. Ohm's experiment loop: selects a short copper wire, places it into the circuit, changes the electrical condition at the source, watches the galvanometer, writes a reading. Each cycle adds a `handwritten measurements` page. VFX galvanometer needle swing.
5. [VO] S07_L05 over the loop → S07_L06 (three cycles, one per "again") → S07_L07 (pages fan out).
6. [VO] S07_L08 → S07_L09.
7. VFX: historical apparatus starts transforming → modern circuit (first flicker only).
8. [HANDOFF] Save camera pose. `Scene07Complete` → Scene 8, match-cut.

Interactions: none. Agent context — Covered: Scenes 5–6 plus Ohm's 1826 investigation (changing the potential difference, observing the current, many times, looking for a pattern), his apparatus. Do not spoil: the pattern, V ∝ I, V = IR, resistance, ohms. Chips: "Why did Ohm repeat it so many times?" · "What is a galvanometer?" · "Why did Ohm use a thermoelectric source instead of a battery?"

---

## Scene 8 — From Ohm's Lab to Our Lab (`Scene08_OurLab`)
Enters from: Scene 7, match-cut. Exits to: Scene 9 (same bench, match-cut).

| ID | Text | Fallback |
|---|---|---|
| S08_L01 | Ohm didn't have the modern instruments sitting in your school laboratory today. | 4.5 s |
| S08_L02 | So let's investigate his relationship using the instruments you would use today. | 4.5 s |

On-screen labels (exact): `AMMETER` / `Measures current — I — ampere (A)` · `VOLTMETER` / `Measures potential difference — V — volt (V)` · `NICHROME WIRE — XY` with `X` and `Y` lighting up at its ends · `cells / controllable DC source` · `switch / key`.

Beats (~35 s):
1. Wattson freezes the historical experiment (Ohm holds still; VFX freeze).
2. [VO] S08_L01.
3. Transformation, one item at a time (old prop scales down, new labelled prop scales up, 0.8 s each): galvanometer → AMMETER with its label · VOLTMETER appears with its label · copper wires fade, one straight NICHROME WIRE — XY appears between two terminals, `X` and `Y` light up · cells / controllable DC source appears · switch / key appears · the wooden table material swaps to the clean modern bench. The lab around stays historical.
4. The `CircuitRig` is present but not yet connected: ammeter and voltmeter sit beside the bench in a parts tray.
5. [VO] S08_L02.
6. [HANDOFF] Save camera pose + `CircuitState`. `Scene08Complete` → Scene 9.

Interactions: none. Agent context — Covered: previous plus ammeter measures current (I, ampere); voltmeter measures potential difference (V, volt); nichrome wire XY is the test conductor; a variable DC source and a key. Do not spoil: series/parallel rules (Scene 9), the pattern, resistance, ohms. Chips: "What is nichrome?" · "What does a voltmeter measure?" · "Why not use Ohm's old instruments?"

---

## Scene 9 — Build the Circuit (`Scene09_BuildCircuit`)
Enters from: Scene 8, match-cut on the bench. Exits to: Scene 10 (same bench, match-cut).

| ID | Text | Fallback |
|---|---|---|
| S09_L01 | That's our test conductor. | 1.8 s |
| S09_L02 | We're going to change the potential difference across this nichrome wire...measure the current through it... | 5.5 s |
| S09_L03 | ...and see whether your prediction was right. | 2.8 s |
| S09_L04 | Everything we're measuring revolves around this wire. | 3.2 s |
| S09_L05 | If we want to know how much current flows through the nichrome wire, we use an ammeter. | 5.0 s |
| S09_L06 | Exactly. An ammeter is connected in series, so the same current passes through it and the test wire. | 5.5 s |
| S09_L07 | And when we need the potential difference between the two ends of the wire... we use the voltmeter. | 5.0 s |
| S09_L08 | That's why a voltmeter is connected in parallel across the nichrome wire. | 4.0 s |
| S09_L09 | Click on the key to close the circuit. | 2.8 s |
| S09_L10 | Investigation ready. | 1.6 s |
| S09_L11 | Remember your prediction? | 1.8 s |
| S09_L12 | Let's find out. | 1.6 s |

On-screen text (exact): current path label `Source → Ammeter → Nichrome Wire XY → Switch → Source` · prompt `Click on the key to close the circuit.` · reminder card `If potential difference increases, what happens to current?`
Interaction design: `buildMode = LearnerPlaces` (default) — the learner drags/taps the ammeter into the glowing series slot and the voltmeter into the glowing parallel slot (one valid slot each, snap on drop, gentle shake and return if dropped elsewhere), then taps the key. `buildMode = AutoPlace` makes the parts fly in by themselves and keeps only the key tap.

Beats (~70 s + interactions):
1. Wattson moves to the nichrome wire XY; it highlights. [VO] S09_L01 → S09_L02 → S09_L03 → S09_L04.
2. Wattson moves to the ammeter in the tray. [VO] S09_L05. The series slot glows.
3. [INTERACTION] `S09_PlaceAmmeter`. On snap: the current path lights up along Source → Ammeter → Nichrome Wire XY → Switch → Source with the label.
4. [VO] S09_L06.
5. Wattson moves to the voltmeter. [VO] S09_L07. Points `X` and `Y` pulse; the parallel slot across XY glows.
6. [INTERACTION] `S09_PlaceVoltmeter`.
7. [VO] S09_L08.
8. [VO + PROMPT] S09_L09 with the on-screen prompt; the key pulses with a tap-hand icon.
9. [INTERACTION] `S09_CloseKey`: tap/click the key. CLICK SFX. `keyClosed = true`; small default source voltage so the ammeter needle moves and the voltmeter activates. The nichrome wire stays visually normal.
10. [VO] S09_L10 → S09_L11. Reminder card appears (optional toggle `showLearnerPrediction`: second line "Your prediction: Increase / Decrease / Stay the same").
11. [VO] S09_L12.
12. [HANDOFF] Save `CircuitState` and camera. `Scene09Complete` → Scene 10.

Agent context — Covered: previous plus: ammeter in series (same current through it and the wire); voltmeter in parallel across the wire's two ends; closing the key completes the circuit. Do not spoil: the readings, the pattern, resistance, ohms. Chips: "Why is the ammeter in series?" · "Why is the voltmeter in parallel?" · "What happens if the key is open?"

---

## Scene 10 — Become the Experimenter (`Scene10_Experiment`)
Enters from: Scene 9, match-cut (circuit complete, key closed). Exits to: Scene 11 (same bench).

| ID | Text | Fallback |
|---|---|---|
| S10_L01 | Let's start low. Set the battery to 1 volt. | 3.0 s |
| S10_L02 | Note the reading of Potential difference in voltmeter. | 3.2 s |
| S10_L03 | Record the current reading in ammeter. | 2.6 s |
| S10_L04 | Now lets keep increasing the battery voltage, and see the readings in voltmeter and ammeter. | 5.5 s |
| S10_L05 | Don't look at me. | 1.4 s |
| S10_L06 | Look at your data. | 1.6 s |
| S10_L07 | What do you notice? | 1.6 s |
| S10_L08 | Good. But I think there's an even better pattern hiding in there. | 4.0 s |
| S10_L09 | Compare just these two. | 1.8 s |
| S10_L10 | What happened to the current when the potential difference doubled? It also doubled | 5.0 s |
| S10_L11 | Now we're getting somewhere. | 2.0 s |

PROPOSED wrong-answer nudge: S10_R_WRONG "Hmm. Look at the table once more. As the potential difference goes up, what is the current doing?" — then re-ask; after a second wrong answer, highlight the V column then the I column row by row and re-ask once more (the third attempt accepts any answer).

On-screen text (exact): prompts `Let's start low. Set the battery to 1 volt.` · `Record the current reading in ammeter.` · table headers `Potential Difference V (V)` | `Current I (A)` · options `A. Current increases as potential difference increases.` · `B. Current decreases.` · `C. There is no pattern.` · highlight `1 V → 0.2 A` and `2 V → 0.4 A` · discovery `V × 2 → I × 2`
Data: 1 V → 0.2 A · 2 V → 0.4 A · 3 V → 0.6 A · 4 V → 0.8 A · 5 V → 1.0 A (voltmeter shows `1.0 V`; ammeter settles at `0.20 A`).

Beats (~90 s + interactions):
1. A voltage controller (slider or dial, 0–5 V in 1 V steps, ≥ 88 px handle) appears by the source. [VO + PROMPT] S10_L01.
2. [INTERACTION] `S10_Set1V`: the learner sets 1 V. Ammeter responds (needle to 0.20 A).
3. [VO] S10_L02. Voltmeter shows `1.0 V`.
4. [VO + PROMPT] S10_L03. The ammeter settles at `I = 0.20 A`.
5. [INTERACTION] `S10_Record`: tap Record (or the ammeter). Row 1 (`1`, `0.2`) animates into the observation table.
6. [VO] S10_L04.
7. Auto sequence: the controller steps to 2 V, 3 V, 4 V, 5 V (1.5 s each); both meters update; each row auto-fills the table.
8. [VO] S10_L05 → S10_L06 → S10_L07.
9. [INTERACTION] `S10_Pattern`: MCQ A/B/C (correct A) → `LearnerSession.s10PatternAnswer`. Wrong → S10_R_WRONG and the retry logic above.
10. [VO] S10_L08. Table rows 1 and 2 highlight with `1 V → 0.2 A` and `2 V → 0.4 A`.
11. [VO] S10_L09 → S10_L10.
12. [INTERACTION (light)] `S10_Discover`: tap the two highlighted rows (or a "×2" chip) to reveal `V × 2 → I × 2`. Auto-reveals after 5 s.
13. [VO] S10_L11.
14. [HANDOFF] Save table + camera. `Scene10Complete` → Scene 11.

Agent context — Covered: previous plus the five readings; current increases when potential difference increases; doubling V doubles I. Do not spoil: V ∝ I as a law statement, the straight-line graph, resistance, V/I = 5, ohms, V = IR. Chips: "Why do we start at 1 volt?" · "Why does the current change when I change the voltage?" · "What does 'record a reading' mean?"

---

## Scene 11 — Turn Data into a Graph (`Scene11_Graph`)
Enters from: Scene 10, match-cut with the table on screen. Exits to: Scene 12.

| ID | Text | Fallback |
|---|---|---|
| S11_L01 | Now we've got our biggest clue. They're all in a line! | 3.5 s |
| S11_L02 | Double the potential difference...and the current doubles. | 3.5 s |
| S11_L03 | And it keeps happening. | 1.8 s |
| S11_L04 | For this conductor that is the nichrome wire, while its temperature remains constant...the potential difference across it is directly proportional to the current flowing through it. | 9.0 s |
| S11_L05 | But there's still one clue we haven't explained. | 2.8 s |

On-screen text (exact): axes `Current, I (A)` (horizontal) · `Potential Difference, V (V)` (vertical) · points `(0.20, 1.0)` `(0.40, 2.0)` `(0.60, 3.0)` `(0.80, 4.0)` `(1.00, 5.0)` · `I → 2I` · `V → 2V` · `V ∝ I`

Beats (~50 s):
1. A giant graph materialises above the bench. The table's I column slides horizontally to become the x-axis; the V column rises vertically to become the y-axis. Axis labels appear.
2. Points plot one by one with their coordinate labels (0.6 s apart). A glowing line draws through them and through the origin.
3. [VO] S11_L01.
4. Two points highlight, e.g. (0.20, 1.0) and (0.40, 2.0), with `I → 2I` and `V → 2V` tags. [VO] S11_L02 → S11_L03. VFX graph pulse.
5. [VO] S11_L04. The card `V ∝ I` appears at the end of the line.
6. [VO] S11_L05.
7. [HANDOFF] Keep the graph and table (Scene 12 shows them again). `Scene11Complete` → Scene 12.

Interactions: none (optional tap-to-continue, default off). Agent context — Covered: previous plus the V–I points lie on a straight line through the origin; for the nichrome wire at constant temperature, V is directly proportional to I; V ∝ I. Do not spoil: resistance, V/I = constant, slope = resistance, ohms, V = IR. Chips: "Why does the line go through the origin?" · "What does 'directly proportional' mean?" · "Why does the temperature have to stay constant?"

---

## Scene 12 — Discover Resistance (`Scene12_Resistance`)
Enters from: Scene 11. Camera dives into the nichrome wire XY (`WireInterior`, nichrome tint, lattice visible). Exits to: Scene 13.

| ID | Text | Fallback |
|---|---|---|
| S12_L01 | We've seen what pushes charge... | 2.4 s |
| S12_L02 | ...and we've measured how much charge flows... | 2.8 s |
| S12_L03 | But if voltage pushes charge...why doesn't charge move through every conductor equally easily? | 5.5 s |
| S12_L04 | The material itself opposes the flow of current. | 3.0 s |
| S12_L05 | That opposition to the flow of electric current is called electrical resistance. | 4.5 s |
| S12_L06 | And the best part? | 1.5 s |
| S12_L07 | You've already measured it. | 2.0 s |
| S12_L08 | The value of V/I is the same every time. | 3.0 s |
| S12_L09 | This constant tells us how strongly the conductor opposes current. | 3.8 s |
| S12_L10 | We call that constant value resistance of the nichrome wire XY. | 3.8 s |
| S12_L11 | And remember the straight line you made in the graph? | 3.2 s |
| S12_L12 | Every straight line has a slope. | 2.2 s |
| S12_L13 | So for a V-versus-I graph, the slope tells us the resistance of the nichrome wire XY. | 4.5 s |
| S12_L14 | Resistance is measured in ohms. | 2.4 s |
| S12_L15 | A conductor has a resistance of one ohm when a potential difference of one volt produces a current of one ampere. | 6.5 s |

On-screen text (exact): `VOLTAGE` · `CURRENT` · `RESISTANCE` · `Symbol: R` · column header `V / I` · `1.0 ÷ 0.20 = 5` · `2.0 ÷ 0.40 = 5` · `3.0 ÷ 0.60 = 5` · `5` · `5` · table headers `V` | `I` | `V/I` with rows `1.0 V | 0.20 A | 5` … `5.0 V | 1.00 A | 5` · `V / I = CONSTANT` · `CONSTANT` → `RESISTANCE — R` · `R = V / I` · `ΔV` · `ΔI` · `Slope = ΔV / ΔI` · `Ω` · `1 Ω = 1 V / 1 A`

Beats (~110 s + interaction):
1. Dive into the nichrome wire. Carriers drift; the lattice (faint grid of ion spheres) becomes visible.
2. [VO] S12_L01 (`VOLTAGE` card) → S12_L02 (`CURRENT` card) → S12_L03.
3. VFX: carriers impeded by lattice (slow, bounce slightly) and the `tunnelRadius` narrowing metaphor (toggle `narrowingMetaphor`, default on). [VO] S12_L04.
4. `RESISTANCE` word forms from the lattice structure. [VO] S12_L05. `Symbol: R` card.
5. [VO] S12_L06. Cross-fade back to the lab bench (toggle `tableInWire` keeps it inside the wire). The graph and table from Scene 11 are visible side by side.
6. [VO] S12_L07. Wattson points at the table. A new column `V / I` appears.
7. Wattson works the first three: `1.0 ÷ 0.20 = 5`, `2.0 ÷ 0.40 = 5`, `3.0 ÷ 0.60 = 5` (1.2 s each), then the last two fill rapidly `5`, `5`. The whole column glows.
8. [VO] S12_L08. Card `V / I = CONSTANT`.
9. [VO] S12_L09. `CONSTANT` morphs into `RESISTANCE — R`, then `R = V / I`.
10. [VO] S12_L10 → S12_L11.
11. On the graph, Wattson draws a triangle between (0.20, 1.0) and (0.60, 3.0) with labels `ΔV` and `ΔI` (ΔV = 2.0 V, ΔI = 0.40 A). [VO] S12_L12.
12. [INTERACTION] `S12_Slope`: `Slope = ΔV / ΔI = ?` MCQ `5` · `0.2` · `2.5` (correct `5`; PROPOSED format). Wrong → shake, show the division `2.0 ÷ 0.40`, re-ask once, then continue.
13. [VO] S12_L13 → S12_L14. Large `Ω` symbol.
14. [VO] S12_L15. Card `1 Ω = 1 V / 1 A`.
15. [HANDOFF] `Scene12Complete` → Scene 13.

Agent context — Covered: previous plus resistance (R) is the material's opposition to current; V/I is constant for the wire; R = V/I; slope of the V–I graph = R; ohm; 1 Ω = 1 V / 1 A. Do not spoil: the name "Ohm's law" / V = IR as the named law (Scene 13), the Christmas-lights answer. Chips: "Why is V divided by I always 5?" · "What is one ohm?" · "Does a thicker wire have more resistance?" (thicker → less resistance; the narrowing is a metaphor).

---

## Scene 13 — The Law Gets Its Name (`Scene13_OhmsLaw`)
Enters from: Scene 12. Exits to: end of the Unity section → Scene 14 is an external video.

| ID | Text | Fallback |
|---|---|---|
| S13_L01 | So now we know all three pieces. | 2.4 s |
| S13_L02 | The push. | 1.2 s |
| S13_L03 | The flow. | 1.2 s |
| S13_L04 | And the opposition. | 1.6 s |
| S13_L05 | And the relationship connecting them... | 2.6 s |
| S13_L06 | And today... we call it OHM'S LAW. | 3.0 s |
| S13_L07 | We've got our law. Now let's test it against the mystery that started all of this. | 4.5 s |

On-screen text (exact): `V — Potential Difference` · `I — Current` · `R — Resistance` · `V = IR` · `OHM'S LAW`

Beats (~35 s + checkpoint):
1. Three cards float: `V — Potential Difference` (blue), `I — Current` (amber), `R — Resistance` (coral). They combine into `V = IR`.
2. [VO] S13_L01 → S13_L02 (V highlights) → S13_L03 (I highlights) → S13_L04 (R highlights) → S13_L05.
3. [VO] S13_L06. `OHM'S LAW` title card. VFX title reveal.
4. [CHECKPOINT] Adaptive checkpoint (enabled by default here; toggle `checkpointEnabled`, `placement` note).
5. [VO] S13_L07.
6. [HANDOFF] `ModuleFlow.UnitySectionComplete` → placeholder card "→ Scene 14 (video)"; play the video if a path is configured.

Adaptive checkpoint: ask Q1; correct → Q2 (harder) → done; wrong → Wattson explains (PROPOSED) and asks the easier question → done. Results in `LearnerSession`.
- AQ1 (storyboard): "If resistance stays constant and voltage doubles, what happens to current?" Options (PROPOSED): A. It doubles · B. It halves · C. It stays the same (correct A)
- AQ2 (storyboard): "Two conductors carry the same current. One requires a larger potential difference. Which has greater resistance?" Options (PROPOSED): A. The one that needs the larger potential difference · B. The one that needs the smaller potential difference · C. Both have the same resistance (correct A)
- AQ_EXPLAIN (PROPOSED): "Remember the table, detective: every time V went up, I went up with it, and V divided by I stayed at 5."
- AQ_EASY (PROPOSED): "When the potential difference across the nichrome wire went up, what happened to the current?" Options: A. It went up · B. It went down · C. Nothing changed (correct A)
- AQ_GOOD (PROPOSED, after a correct answer): "Case closed, detective."

Agent context — Covered: everything, including Ohm's law V = IR. Do not spoil: the Christmas-lights explanation (Scene 14). If asked, hint: think about which battery could keep a bigger potential difference across the lights. Chips: "What does V = IR mean?" · "Can I use Ohm's law for any wire?" · "Who was Ohm?"

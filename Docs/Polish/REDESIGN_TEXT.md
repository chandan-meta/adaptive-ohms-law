# Revised dialogue and panel text (2026-10-06 redesign)

Every spoken line, every panel instruction, note, formula and reveal, in the order the learner meets them. Lines
marked *proposed* were written for the redesign (they live in `SceneNN/Dialogue/Proposed/`); the others are the
original script, kept or shortened. All lines are Wattson's unless noted. Voice: edge-tts `en-US-AnaNeural`
(rate +10 %, pitch −4 Hz), baked by `Ohm's Law ▸ Bake VO ▸ All Scenes`.

Panel text appears in the single right-hand lesson panel: **instruction** (one at a time, until the action is done),
**note** (a short supporting fact, one at a time), **formula** (lines that accumulate under the data), **reveal** (a
hero line). Captions of the spoken lines appear in the same panel.

## Scene 4 — Meet Wattson and Georg Ohm (Ohm's study, 1826)

| Id | Line |
|---|---|
| S04_L01 | Welcome, detectives. |
| S04_L02 | Your Christmas lights gave you a question. Nearly two centuries ago, Georg Ohm was investigating the relationship that can help us answer it. |
| S04_L03 | He wanted to know how these two are connected. *(VOLTAGE and CURRENT light up on the wall)* |
| S04_L04 | But before we understand his experiment... |
| S04_L05 | ...you need to see what electricity looks like from the inside. |
| S04_L06 | Everybody in. |

Instruction (held until the learner taps): **Click or tap the highlighted wire to explore what happens inside.**

## Scene 5 — Inside the wire

| Id | Line |
|---|---|
| S05_L01 | These are electric charges. |
| S05_N01 *proposed* | Free electrons, inside the copper wire. These electrons are already moving around. But there's no overall flow in one direction yet. |
| S05_W02 *proposed* | This end connects to the battery's negative terminal. |
| S05_W03 *proposed* | That end, to the positive terminal. |
| S05_N02 *proposed* | Now we apply a voltage: a difference in electric potential between the two ends. |
| S05_N03 *proposed* | That creates the electrical push that gives the electrons an overall direction. They still jiggle, but now they all drift together. |
| S05_N04 *proposed* | Watch this section. One, two, three... Now count how much charge passes in one second. |
| S05_L06 | And when electric charge flows... |
| S05_L07 | ...we call that electric current. |
| S05_N05 *proposed* | That rate of charge flow, the charge passing each second, is the current. |
| S05_L08 | And that's exactly the clue we need for your Christmas lights. The new battery changed the electrical conditions—but before we solve your mystery, we need to find out exactly how voltage and current are related. |
| S05_L09 | Want to predict what we'll find? |

Notes (one at a time):
- **Free electrons** — Inside a metal some electrons are free to move. Before a voltage is applied they jiggle randomly: no overall flow in any direction.
- **Voltage (V)** — The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).
- **Electron drift** — The free electrons keep jiggling, and now they all drift from the − end toward the + end. Conventional current is drawn the other way: + → −.
- **Current (I)** — The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).

Counter block: **CHARGE THROUGH THE SECTION** · the count this second · "last second: N charges" · "charge ÷ time = current (I)" ·
"Simplified: a few representative electrons, slowed down so you can count them. The real wire carries billions per second."

## Scene 6 — Your prediction

| Id | Line |
|---|---|
| S06_L01 | Detective test. |
| S06_L02 | If we increase the potential difference while everything else stays the same, what do you predict will happen to the current? |
| S06_R_A *proposed* (after A) | A bold prediction, detective. Hold on to it. |
| S06_R_B *proposed* (after B) | A surprising theory. Every good detective tests a theory. |
| S06_R_C *proposed* (after C) | A careful guess. Let's put it to the test. |
| S06_L03 | Interesting. Remember your answer. Let's see if Ohm's experiment agrees with you. |

Instruction: **Tap your prediction.** Options: A Increase · B Decrease · C Stay the same (a prediction, never marked right or wrong).

## Scene 7 — Back to Georg Ohm

| Id | Line |
|---|---|
| S07_P01 *proposed* | And we're back! That's the very wire we just explored. |
| S07_L01 | Now you know the two clues. |
| S07_L04 | Is there a mathematical relationship between voltage and current? |
| S07_L05 | In 1826, Ohm kept changing one electrical condition, the potential difference and observing what happened to the current. |
| S07_L07 | He wasn't looking for one reading. He was looking for a pattern. |
| S07_L09 | So let's run the investigation ourselves. |

Notes: **Torsion galvanometer** — A needle twists when current flows · **Notebook** — Ohm's handwritten measurements · **Source** — a thermocouple: a steady push.

## Scene 8 — Ohm's lab becomes our lab

| Id | Line |
|---|---|
| S08_L01 | Ohm didn't have the modern instruments sitting in your school laboratory today. |
| S08_P01 *proposed* | The instruments changed, detectives. The relationship we're testing is still the same. |
| S08_P02 *proposed* | Thank you, Georg Ohm! Back to 1826 you go. |
| S08_L02 | So let's investigate his relationship using the instruments you would use today. |

Notes as each instrument appears: **Ammeter** — Measures current — I — ampere (A) · **Cells / controllable DC source** — Provides a controllable
potential difference · **Nichrome wire — XY** — Test conductor between X and Y · **Switch / key** — Completes or breaks the circuit ·
**Voltmeter** — Measures potential difference — V — volt (V).

## Scene 9 — Build the circuit

| Id | Line |
|---|---|
| S09_L01 | That's our test conductor. |
| S09_L02 | We're going to change the potential difference across this nichrome wire...measure the current through it... |
| S09_L03 | ...and see whether your prediction was right. |
| S09_L05 | If we want to know how much current flows through the nichrome wire, we use an ammeter. |
| S09_L06 | Exactly. An ammeter is connected in series, so the same current passes through it and the test wire. |
| S09_Conventional *proposed* | Plot twist: we draw conventional current plus to minus. Electrons drift minus to plus! |
| S09_L07 | And when we need the potential difference between the two ends of the wire... we use the voltmeter. |
| S09_L08 | That's why a voltmeter is connected in parallel across the nichrome wire. |
| S09_L09 | Close the switch to complete the circuit. |
| S09_L10 | Investigation ready. |
| S09_L11 | Remember your prediction? |
| S09_L12 | Let's find out. |

Instructions, in order: **Select the ammeter.** → **Click the highlighted position to place it.** → **Select the voltmeter.** →
**Click the highlighted position to place it.** → **Close the switch to complete the circuit.**
Schematic note once the key closes: "Conventional current is drawn + → − (electrons drift the other way)".
Reminder block: the Scene 6 question and "Your prediction: Increase / Decrease / Stay the same".

## Scene 10 — The experiment

| Id | Line |
|---|---|
| S10_L01 | Let's start low. Set the battery to 1 volt. |
| S10_Set2V_GO *proposed* | One volt, and the ammeter reads 0.20 amperes. Now set the voltage to 2 volts. |
| S10_Set3V_GO *proposed* | Now 3 volts. |
| S10_Set4V_GO *proposed* | And 4 volts. |
| S10_Set5V_GO *proposed* | Last one: 5 volts. |
| S10_SetNV_H *proposed* (hints after a pause) | Tap the 1 V button in the panel. / Tap the 2 V button. … |
| S10_L06 | Look at your data. |
| S10_L07 | What do you notice? |
| S10_L09 | Compare just these two. |
| S10_L10 | What happened to the current when the potential difference doubled? It also doubled |
| S10_Callback_If_S06_Prediction_A *proposed* | Back in the wire you predicted the current would rise. Your data agrees. |
| S10_Callback_If_S06_Prediction_B *proposed* | You predicted the current would drop. The data says it rises. Good detectives update. |
| S10_Callback_If_S06_Prediction_C *proposed* | You predicted no change. The data shows it rises with the push. |
| S10_L11 | Now we're getting somewhere. |

Instructions per reading: **Set the voltage to 1 V.** (buttons 1 V … 5 V; the ones above the step stay dim) → **Observe the current reading.**
then the row records and the point plots; then **Set the voltage to 2 V.** … **5 V.**
Formula lines: `1 V → 0.20 A     2 V → 0.40 A` · `V × 2   →   I × 2`

## Scene 11 — The pattern

| Id | Line |
|---|---|
| S11_L01 | Now we've got our biggest clue. They're all in a line! |
| S11_N01 *proposed* | Look at the pattern! Double the voltage, and the current doubles too. They change together in the same proportion. |
| S11_L04 | For this conductor that is the nichrome wire, while its temperature remains constant...the potential difference across it is directly proportional to the current flowing through it. |
| S11_L05 | But there's still one clue we haven't explained. |

Reveal: **V ∝ I** — directly proportional, for this wire at constant temperature.

## Scene 12 — Resistance

| Id | Line |
|---|---|
| S12_L03 | But if voltage pushes charge...why doesn't charge move through every conductor equally easily? |
| S12_N01 *proposed* | Keep the voltage the same, but change the wire material. This one allows more current. The other opposes the flow more. |
| S12_L05 | That opposition to the flow of electric current is called electrical resistance. |
| S12_L06 | And the best part? |
| S12_L07 | You've already measured it. |
| S12_L08 | The value of V/I is the same every time. |
| S12_L09 | This constant tells us how strongly the conductor opposes current. |
| S12_L10 | We call that constant value resistance of the nichrome wire XY. |
| S12_L11 | And remember the straight line you made in the graph? |
| S12_L12 | Every straight line has a slope. |
| S12_L13 | So for a V-versus-I graph, the slope tells us the resistance of the nichrome wire XY. |
| S12_L14 | Resistance is measured in ohms. |
| S12_N02 *proposed* | Written with this symbol: omega. |
| S12_L15 | A conductor has a resistance of one ohm when a potential difference of one volt produces a current of one ampere. |
| S12_N03 *proposed* | Our nichrome wire needs five volts for one ampere, so its resistance is five ohms. |

Inset: **MAGNIFIED · same voltage · same length · same thickness · same temperature** — Copper (MORE current) / Nichrome (LESS current) —
"More opposition = more resistance. The charges still get through, just more slowly."
Note: **Resistance (R)** — How much a conductor opposes the flow of current. Same voltage, less current = more resistance. The charges are not lost: they just get through more slowly.
Table column: V / I with the working `1.0 ÷ 0.20 = 5` … then `5`, `5`.
Formula lines: `V ∝ I` · `V = k I` · `V ÷ I = k   (the same 5 every time)` · `k = R   (the resistance)` · `V = I R` · `slope = ΔV ÷ ΔI = 2.0 V ÷ 0.40 A = 5`
Reveal: **Ω** — the ohm: the unit of resistance (R). Then `1 Ω = 1 V ÷ 1 A` · `this wire:  5.0 V ÷ 1.00 A = 5 Ω`.

## Scene 13 — Ohm's law

| Id | Line |
|---|---|
| S13_L01 | So now we know all three pieces. |
| S13_L02 | The push. |
| S13_L03 | The flow. |
| S13_L04 | And the opposition. |
| S13_L05 | And the relationship connecting them... |
| S13_L06 | And today... we call it OHM'S LAW. |
| S13_N01 *proposed* | You've followed the charges, measured the current, and discovered the pattern. Voltage, current, and resistance are connected by one relationship: V equals I times R. That's Ohm's law. Well done, detectives. |

Formula lines: `V   —   the push: potential difference` · `I   —   the flow: current` · `R   —   the opposition: resistance`.
Reveal: **V = I R**, then **OHM'S LAW**. End block: REPLAY · EXIT.

## Ask Wattson (in the panel)

Button text: **ASK WATTSON** / **BACK TO THE LESSON**. Status line: LISTENING… / THINKING… / SPEAKING / RESTING, with the mode line
"OFFLINE MODE: saved answers only (the Wattson server needs its API keys)" until the proxy is configured. Wattson receives the scene's
context, the current instruction, the current note and the question on screen (see `WattsonAgent`).

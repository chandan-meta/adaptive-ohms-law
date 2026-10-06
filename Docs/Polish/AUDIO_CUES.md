# Audio cue plan (2026-10-06 redesign)

Every sound is synthesized in code at build time (`Assets/OhmsLaw/Editor/SfxBank.cs`, merged into Scene 4's
`BuildSfx`), so the whole bank is licence-free and reproducible. Clips land in `Assets/OhmsLaw/Scene04/Audio/SFX/`
and every scene's director gets them by name (`SceneDirector.sounds`). Scenes play them with the Timeline cues
`sfx <name>`, `ambience <name>|off` and `music <name>|off`; the gates play their own confirmations in code.
Everything is ducked under the voice by `AudioDucker` (music to 30 %, effects to 55 %) and the voice channel keeps
Wattson from ever speaking over himself. Each one-shot plays once per discrete action, so repeated taps never stack.

## Beds (loops, crossfaded by the cues)

| Clip | Where | Character |
|---|---|---|
| `StudyTone` | Scenes 4, 7, 8 (Ohm's study) | sparse fireplace crackle over quiet room tone |
| `RoomTone` | Scenes 8 (after the sweep) to 13 | faint fluorescent hum in a quiet lab |
| `WireHum` | Scenes 5, 6 (inside the wire) | low 55 Hz hum with a slow wobble: the microscopic ambience |
| `PadLab` | Scenes 4, 7 to 13 | warm, slow major pad, quiet |
| `PadWire` | Scenes 5, 6 | darker minor pad for the inside of the wire |

## One-shots

| Clip | Moment | Played by |
|---|---|---|
| `Enter` | the dive into the wire (Scene 4's tap, Scene 5's push) | Scene 4 Timeline SFX track; Scene 5 `sfx Enter` |
| `Exit` | the Ohm-niscope flash out of the wire (Scene 6) | `sfx Exit` |
| `Highlight` | a wire / terminal / instrument lights up | `sfx Highlight` (Scenes 5, 7, 8, 9, 12) |
| `Count` | each charge the cross-section counter counts | `wire.ticks 1` → `SceneDirector.Tick` (Scenes 5, 6), pitch varies slightly |
| `VoltStep` | a voltage button sets the source (pitched up per volt); the + terminal's gauge rise in Scene 6 | `LearnerVoltage`, `sfx VoltStep` |
| `Select` | the learner selects an instrument or an answer | `PlacePart` (first selection), `Prediction` |
| `Place` | an instrument snaps into its slot; the voltmeter joins in Scene 8 | `PlacePart`, `sfx Place` |
| `Switch` | the key closes | `CloseKey` |
| `Record` | a row records into the table | `SetVoltage` gate |
| `Plot` | a point lands on the graph; a V / I cell works out (pitched per row) | `SetVoltage` gate, `table.work` / `table.result` |
| `Rise` | the line sweeps through the points; the V ∝ I reveal | `graph.line 1`, `sfx Rise` |
| `Reveal` | a formula line or a reveal appears; the rise/run marker; Ohm's farewell dust | `formula`, `reveal`, `graph.triangle`, `sfx Reveal` |
| `Omega` | the Ω symbol | `sfx Omega` (Scene 12) |
| `Transform` | each 1826 instrument becomes its modern equivalent; the first flicker in Scene 7 | `Becomes()`, `sfx Transform` |
| `LabSweep` | the study sweeps into the classroom | `sfx LabSweep` (Scene 8) |
| `Finale` | V = IR resolves into OHM'S LAW | `sfx Finale` (Scene 13) |
| `Tap` | reserved for a generic UI tap | unused by the scenes (available) |
| `Connect` | reserved for a lead touching a terminal | unused by the scenes (the voltmeter's leads appear with `Place`) |

Scene 4 keeps its original synthesized cues (`Whoosh`, `Zip`, `Pop`, `Zap`, `Crackle`, `Shimmer`, `Click`, `Beam`) on
its Timeline SFX tracks, with `PadLab` and `StudyTone` as its beds.

## Not done here

- No recorded (foley) material exists in the project; every cue is synthesized. A sound designer can drop a wav with
  the same name into `Scene04/Audio/SFX/` and the builders keep it (an existing file is never overwritten).
- Nothing is spatialized (2D buses only), so a tablet with one speaker hears the same mix as a desktop.

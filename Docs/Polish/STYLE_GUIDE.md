# Scene 4 Style Guide for Scenes 5-13

Source of every value below: `Assets/OhmsLaw/Editor/Scene04Builder.cs`, `Scene04Builder.Timeline.cs`, `Assets/OhmsLaw/Scene04/Scripts/Scene04Director.cs`, and `Assets/OhmsLaw/Shared/{WattsonController,PopIn,Niscope,ModulePalette,UI,SubtitleUI,CinematicControlsUI,FlickerLight,ElectricArc,WorldText,ModuleFlow,MCQPanel}.cs` plus `Shared/Agent/AskWattsonUI.cs`. Compared against `Editor/SceneBuilderBase.cs`, `SceneBuilderBase.Stages.cs` and `BlockoutAssets.cs` to see what the blockout builders already have.

---

## 0. Coordinate frame of the study (reuse verbatim in Scenes 7 and 8)

Scene 4 builds everything in the lab model's own coordinates (`Assets/AdaptiveLearning/Art/env/Environment.fbx`, instantiated at the origin, unscaled, saved as variant `Scene04/Prefabs/Lab_Scene04.prefab`).

| Constant | Value | Meaning |
|---|---|---|
| `FloorY` | `-1.08` | floor top |
| `DeskY` | `-0.26` | Ohm's desk top |
| `Group` | `(-0.2, 0.27, 0.3)` | eye point of the arriving kids (camera POV, what Wattson looks at) |
| `Hover` | `(-1.5, 0.2, -0.45)` | where Wattson settles |
| `OhmFeet` | `(-2.4, -1.08, -2.36)` | Ohm model root, scale `1.55`, faces the room (no rotation set) |
| `OhmHead` | `(-2.4, 0.45, -2.2)` | "Ohm Point" (look/point target, camera focus) |
| `Wire` | `(-2.71, -0.18, -1.9)` | centre of the copper test wire (half-length 0.21 along X) |
| `Notebook` | `(-2.22, -0.252, -2.03)` | notebook cube, yaw 8 deg |
| `Galvanometer` | `(-2.6, -0.26, -2.02)` | galvanometer base |
| `Pile` | `(-2.9, -0.26, -2.03)` | voltaic pile base |
| `WordsCamera` | `(0.06, 0.3, 0.45)` | SH05 camera; word cards are oriented toward this point |
| OhmWorker targets | notebook `Notebook + (0,0.02,0)`; galvanometer `Galvanometer + (0.02,0.08,-0.02)`; wirePost `Wire + (0.21,0.03,-0.03)` | |
| `deskLook` | `(-2.0, -0.25, -1.8)` | what SH01 looks at |
| Words centre | `Hover + (0,0.46,0)` = `(-1.5, 0.66, -0.45)` ("Words Point") | |
| VOLTAGE / CURRENT | `centre -/+ right*0.52`, `right = Cross(up, -ProjectOnPlane(WordsCamera-Hover))` -> approx `(-1.24, 0.66, -0.90)` and `(-1.76, 0.66, 0.00)` | |
| "?" | `centre + (0,0.22,0)` | |
| Words drift (SH07) | `up*0.28 + normalize(ProjectOnPlane(Hover-WordsCamera))*0.3` ≈ `(-0.26, 0.28, -0.15)`, lerp rate `1-exp(-2 dt)` | |
| Wattson flight spline knots | `(-3.0,-0.6,-2.38)` hidden behind desk -> `(-2.95,0.3,-2.2)` over pile -> `(-2.6,0.8,-1.3)` -> `(-1.75,0.62,-0.95)` -> `(-1.2,0.0,-0.8)` dip -> `Hover`; `TangentMode.AutoSmooth` | |
| Lights | Oil Lamp `(-1.7,0.16,-1.72)`, Desk Candle `(-2.36,0.1,-1.9)`, Fireplace `(-1.35,-0.5,-3.15)`, Bookcase Candle `(-2.85,0.22,0.87)`, Group Fill `(0,0.85,0.55)` | |
| Reflection probe | centre `(-1.45,0.3,-1.0)`, size `(3.9,2.9,4.9)` | |
| Light probes | grid x∈{-2.9,-1.5,0}, y∈{-0.8,0.3,1.3}, z∈{-2.9,-1.3,0.6} | |
| God-ray window | sheets at x=-3.3, z from -3.05 to -2.2, y from -0.1 to 1.45 | |
| Main camera start | `(0.15, 0.75, 0.95)` | |

**Mismatch to fix:** the blockout base (`SceneBuilderBase.Stages.cs`) uses a different frame: floor y=0, `BenchTop = 0.9`, `OhmFeet = (0,0,1.1)`, `WattsonHome = (-1.3,1.6,-0.9)`, bench at origin. Scenes 7 and 8 must switch to the Scene 4 constants above (instantiate the same `Lab_Scene04` variant at the origin, same `OhmFeet`/scale 1.55, same apparatus positions) so the study, desk and camera rig are literally the same as Scene 4. Also note the apparatus differs: Scene 4 shows voltaic pile + galvanometer + one test wire; the blockout `OhmTable()` shows a thermoelectric source, mercury cups and four wires per the storyboard. Keep Scene 4's three objects at their positions and add the storyboard extras around them on `DeskY`.

Lab model dressing rules (in `BuildStage`): disable every `Light` in the FBX; deactivate `chock_and_duster`, `glass_and_ice_cube_`, `Book_Pile_1`; renderers named `Flame*` -> `ShadowCastingMode.Off` and material replaced by `EmissiveCopy(..., "Flame_Candle"|"Flame_Fire", new Color(4, 1.9, 0.55))`; `Window*`/`Wall_Window*` -> shadows Off; everything else -> `TwoSided`; all renderers get `StaticEditorFlags.BatchingStatic | ReflectionProbeStatic`.

---

## 1. Lighting rig

**Render settings**
- `skybox = null`; `ambientMode = Trilight`: sky `(0.16,0.18,0.26)`, equator `(0.2,0.15,0.11)`, ground `(0.07,0.05,0.04)`
- Fog on, `Exponential`, colour `(0.3,0.24,0.2)`, density `0.03`

**Lights** (all under a `Lighting` node)

| Light | Type | Colour | Intensity | Range | Shadows | Extra |
|---|---|---|---|---|---|---|
| Window Light | Directional, rot `Euler(28, 82, 0)` | `(0.74,0.84,1.0)` | 2.4 | - | Soft | the only shadowed directional |
| Oil Lamp | Point | `(1,0.6,0.28)` | 3.2 | 4.0 | Soft | `FlickerLight` (speed 7, amount 0.3) |
| Desk Candle | Point | `(1,0.6,0.28)` | 1.6 | 2.4 | None | `FlickerLight` |
| Fireplace | Point | `(1,0.6,0.28)` | 4.5 | 4.5 | None | `FlickerLight.speed = 4` |
| Bookcase Candle | Point | `(1,0.6,0.28)` | 1.4 | 3.0 | None | `FlickerLight` |
| Group Fill | Point | `(1,0.9,0.78)` | 2.0 | 5.0 | None | fill from the kids' side |
| Wattson Follow Light | Point, child of Wattson | `(1,0.95,0.8)` | 0.7 | 2.2 | None | |
| Niscope Lens Light | Point, child of gadget | `wattsonAccent` | 0 (driven by `Niscope`: `6*burst + 0.8*glow`) | 1.8 | None | |

Rule: exactly two realtime shadow casters (window + oil lamp). `ModuleFlow` strips shadows from all non-directional lights on mobile and halves `maxParticles`.

**Reflection probe**: `Baked` (realtime has no texture in edit mode under Forward+), `boxProjection = true`, resolution 128, baked by `Lightmapping.BakeReflectionProbe(probe, S4 + "/VFX/Scene04_Reflection.exr")` after the scene is assembled. **Light probe group**: 27-point grid (see table).

**God rays**: a hand-built mesh (`MeshAsset("GodRays")`) of 6 slanted quads through the window opening, extruded along `window.transform.forward * 3.4`, UVs `(0,0.5),(1,0.5),(1,1),(0,1)` so the soft particle texture fades along the length; material `Glow("Glow_GodRay", new Color(0.6,0.74,1,0.16))`; shadows off.

**Camera background**: `SolidColor (0.5, 0.63, 0.85)` (pale sky through the window), `allowHDR = true`.

---

## 2. Post-processing

**Global volume** (`Profile("Scene04_Global")`, `isGlobal`):
- `Tonemapping.mode = ACES`
- `Bloom`: threshold `1.0`, intensity `0.85`, scatter `0.72`
- `Vignette`: intensity `0.26`, smoothness `0.45`
- `FilmGrain`: `Thin1`, intensity `0.22`
- `ColorAdjustments`: postExposure `0.25`, contrast `8`, saturation `6`

`Override<T>` resets every parameter's `overrideState` to false first, so only the values above are overridden; the rest keep URP defaults.

**Per-shot lens profiles** (`Lens(name, focalLength, aperture, dive, motionBlur)`), all `DepthOfField.mode = Bokeh`, `focusDistance = 2` (ignored at runtime; `CinemachineVolumeSettings.FocusTracking = CustomTarget` drives it from the shot's focus transform so blends rack focus):

| Profile | Focal length | Aperture | Extras |
|---|---|---|---|
| `Lens_Wide` | 28 | f/5.6 | |
| `Lens_Medium` | 45 | f/3.2 | |
| `Lens_Shallow` | 70 | f/1.8 | |
| `Lens_Tracking` | 35 | f/4.0 | `MotionBlur.intensity = 0.45` |
| `Lens_Dive` | 30 | f/2.8 | `MotionBlur 0.9`, `ChromaticAberration 0.9` |

Main camera: `UniversalAdditionalCameraData.renderPostProcessing = true`, `antialiasing = SMAA`, near `0.05`. (Blockout base currently has post **off** and bg `(0.12,0.13,0.16)` -> flip to Scene 4 settings.)

---

## 3. Camera conventions

- `CinemachineBrain.DefaultBlend = Cut, 0`. All movement comes from Timeline clip overlap, not the brain default. (Blockout base uses `EaseInOut 1s`; change to Cut + explicit overlaps.)
- `Vcam(...)`: `LensSettings.Default`, FOV per shot, near `0.05`, far `60`, `Priority 0` (first shot gets 10). Optional `TrackingTarget` + `CinemachineRotationComposer` for dolly shots. Optional `CinemachineBasicMultiChannelPerlin` with the `Handheld_normal_mild` NoiseSettings, `AmplitudeGain = noise`, `FrequencyGain = 0.8`. Every vcam gets `CinemachineVolumeSettings` with the lens profile and `FocusTarget`.
- **Move pattern**: a shot with motion is two vcams A and B; clip A spans `[start, end-0.05]`, clip B `[start+0.05, end]`; blend durations are then set equal to the overlap, so the whole shot is one long blend (dolly/push/orbit). Rack focus (SH04) is three clips: A `[start, start+2.6]`, B `[start+1.0, end-0.5]`, A again `[end-1.8, end]`.
- Timeline frame rate 30; `PlayableDirector.playOnAwake`, `extrapolationMode = Hold`.
- Shot plan: shots stretch to `max(planned, lineAt + line.Duration + 0.2)` when the recorded VO is longer; everything later shifts.

| Shot | FOV | Lens | Noise | Framing |
|---|---|---|---|---|
| SH01a/b Arrival | 50 -> 46 | Wide | 1.4 -> 0.6 | `(0.15,0.75,0.95)` -> `(-0.15,0.42,0.55)`, look `deskLook`, focus Ohm |
| SH02a/b Wattson Enters | 45 -> 42 | Tracking | - | tracks Wattson, `(-0.45,0.35,0.15)` -> `(-0.3,0.3,0.3)` |
| SH03 Welcome | 32 | Shallow | - | `Hover + toGroup*0.95 - up*0.12`, slightly below |
| SH04a Over Shoulder / SH04b Rack | 38 / 36 | Shallow | - | `Hover + fromOhm*0.85 + shoulder(0.3) + up*0.15`; B is 0.35 closer, focus Ohm |
| SH05 Words | 44 | Medium | - | `WordsCamera`, look `Hover + up*0.3` (fits 4:3) |
| SH06a/b Orbit | 44 -> 42 | Medium | - | radius 1.8, yaw -12.5 -> +12.5 deg, +0.15 up, focus Words Point |
| SH07 Before | 38 | Medium | - | `Hover + toGroup*1.4 - up*0.04` |
| SH08a/b Hero | 36 -> 34 | Shallow | - | low angle `-up*0.42/0.38`, push 0.98 -> 0.74 |
| SH09a/b Aim | 30 / 30 | Shallow / Medium | - | over shoulder toward wire, then `Wire - toWire*1.05 + up*0.12` |
| SH10 Everybody In | 30 | Shallow | - | `Hover + toGroup*0.72 + up*0.03` |
| SH11a/b Dive | 22 -> 82 | Medium -> Dive | - | dolly-zoom `Wire - toWire*1.5` -> `Wire - toWire*0.16` |

Rules of thumb: close-ups 30-36 FOV with `Shallow`; mediums 38-45 with `Medium`; establishing 46-50 with `Wide` and handheld noise; dolly-zoom = 22 -> 82.

---

## 4. Materials

Palette (`ModulePalette.asset`): voltage `#3FA9FF`, current `#FFB830`, resistance `#FF5A5F`, charge `#D6FAFF`, wattsonAccent `#BFF6FF`, panel `(0.07,0.09,0.13,0.82)`.

Helpers:
- `Lit(name, color, metallic, smoothness, emission?)` -> URP/Lit, optional `_EMISSION` + `RealtimeEmissive` GI flag.
- `Glow(name, hdrColor, softTexture=true)` -> `URP/Particles/Unlit`, additive (`_Surface 1, _Blend 2, SrcAlpha/One`, alpha blend One/One), `_ZWrite 0`, `_Cull Off`, Transparent queue, `_BaseMap = Default-Particle.psd` when soft (lines/reticle use `softTexture:false`).
- `EmissiveCopy(source, name, emission)` -> copy of a project material with `_EMISSION`, `_EmissionMap = _BaseMap`.
- `Hdr(color, intensity)` -> multiply RGB only.

| Material | Recipe |
|---|---|
| Copper | Lit `(0.86,0.46,0.26)`, metallic 1, smooth 0.72 |
| Brass | Lit `(0.83,0.66,0.3)`, 1, 0.68 |
| Zinc | Lit `(0.62,0.65,0.68)`, 1, 0.5 |
| DarkWood | Lit `(0.2,0.11,0.06)`, 0, 0.4 |
| Paper | Lit `(0.86,0.8,0.66)`, 0, 0.15 |
| Ink | Lit `(0.08,0.07,0.1)`, 0, 0.3 |
| NiscopeRing | Lit brass colour, 1, 0.75, emission `Hdr(accent, 2.2)` |
| Flame_Candle / Flame_Fire | EmissiveCopy, emission `(4, 1.9, 0.55)` |
| Wattson_Glow | EmissiveCopy of `LB_geo` material, emission black (driven at runtime) |
| Glow_WattsonSpark | `Hdr(accent, 3)` |
| Glow_Lens | `Hdr(accent, 2.5)` |
| Glow_Beam | `Hdr(accent, 6)` |
| Glow_Reticle | `Hdr(accent, 2.2)`, hard |
| Glow_ChargeStreamsBlue/Gold | `Hdr(voltage|current, 3.5)` |
| Glow_Dust | `(0.8,0.68,0.5,0.2)` |
| Glow_Motes | `(0.9,0.85,0.75,0.55)` |
| Glow_Spark{Voltage,Current,Question} | `Hdr(color, 4)` |
| Glow_Arc | `Hdr(lerp(voltage,current,0.5) + white*0.3, 5)`, hard |
| Glow_Shimmer | `Hdr(current, 4)` |
| Glow_Streaks | `Hdr(lerp(accent,current,0.3), 5)` |
| Glow_GodRay | `(0.6,0.74,1,0.16)` |
| Outline_WireMask / Outline_Wire | shader `AdaptiveLearning/EquipmentOutline`: mask `_Width 0, _StencilComp 8, _StencilPass 2, _ColorMask 0, queue 2990`; hull `_Color Hdr(current,2.5), _Width 5, _StencilComp 6, _StencilPass 0, _ColorMask 15, queue 3000` (both on a duplicate of the mesh) |

HDR intensity bands: ~2.2-2.6 for faces and rings, 3-4 for particle sparks, 5-6 for arcs/beams/streaks (bloom threshold 1.0).

---

## 5. Floating word cards (`Word(...)`)

- `TextMeshPro` (world space), font `Shared/UI/Nunito SDF.asset` (NunitoSans-Bold, 90pt sampling, padding 9, SDFAA, 1024 atlas; ASCII + `â€¦â€“â€”â€™â€˜â€œâ€?Â°Î©â–¶`; blockout adds a DejaVu `Symbols SDF` fallback for `Î©âˆ?Î”âˆ†â†’Ã·Ã—âˆ’Â±Â·Â°`).
- `fontSize 1.3` (words), `2.4` for "?"; `Bold`, `Center`, `NoWrap`, `rectTransform.sizeDelta = (1.4, 0.4)`.
- Oriented `LookRotation(position - WordsCamera)` (faces the SH05 camera).
- Material per word (`Materials/Word_<text>.mat`, copy of font material): shader `TextMeshPro/Distance Field`, `_FaceColor = Hdr(color, 2.6)`, `GLOW_ON`, `_GlowColor = (color, a 0.6)`, `_GlowOuter 0.45`, `_GlowPower 0.6`.
- `PopIn`: duration `0.55`, overshoot `1.45`, bob `0.025` (bobSpeed default 1.2); "?" adds `pulse 0.14`, `pulseSpeed 5`.
- Spark burst child ("Sparks"): `Glow(Hdr(color,4))`, non-looping, duration 1, lifetime 0.4-0.9, speed 0.6-2.2, size 0.015-0.05, gravity 0.4, burst 60 at t=0, sphere r 0.2.
- Starts inactive; Timeline `ActivationTrack` shows it (VOLTAGE at SH05+0.7 with Pop+Zap SFX, CURRENT at +1.7), both hidden at SH11+1.0. Drift via `Scene04Director.DriftWords` (see section 0).
- Arc between them: `ElectricArc` on a 14-point `LineRenderer`, width 0.012, endpoints at local `(±0.36,0,0)` of the two words; defaults jitter 0.05, cycle 1.3 s, maxReach 0.92, 12 % flicker-off.

For comparison the blockout `WorldText.Card` uses `fontSize = height*14` and a dark backing quad; the polish pass should route `Card(...)` through the Scene 4 recipe (HDR face + glow + PopIn + spark burst, no backing).

---

## 6. Wattson

- Prefab `Assets/AdaptiveLearning/Prefabs/Wattson/Wattson.prefab`; strip all existing `MonoBehaviour`s; `localScale = 0.34`; rotation `LookRotation(ProjectOnPlane(Group - Hover))`; add `WattsonController` + `Animator` (Timeline animates `pathT`).
- Rig names: `Wattson LB Visual`, `right_Sholder_jnt`, `right_elbow_jnt`, `left_Sholder_jnt`, `left_elbow_jnt`, `right_wrist_jnt`, `LB_geo` (SkinnedMeshRenderer, blend shape `blendShape1.Mouth`).
- Glow: `glowRenderers = { LB_geo }`, `glowColor = (1, 0.92, 0.7)`; runtime emission = `glowColor * (0.45 + loudness*0.8)` via MaterialPropertyBlock.
- Flight: spring `60`, damping `9`, bob `0.12` @ `1.6`, bank `14`; `pathT` curve keys `(0,0)`, `(SH02+0.3, 0)`, `(SH02+3.5, 1, inTangent 0.6)`, `(total, 1)` so the spring overshoots and settles. Blockout scenes use `MoveTo(marker)` instead.
- Spark Trail: `Glow(Hdr(accent,3))`, lifetime 0.35-0.8, speed 0.05-0.5, size 0.015-0.05, World sim space, max 300, rate 4/s + 45/m, sphere r 0.12. Fun-tap barrel roll emits 28 + chirp.
- Follow Light: see lighting table.
- Audio children: `Voice (scripted)`, `Voice (live answers)` (Voice bus), `Chirps` (Sfx bus).
- **Ohm-niscope** (built at `right_wrist_jnt`, rotation `LookRotation(wrist - elbow, body forward)`, then parented to the wrist, hidden until `gadgetOut`):
  - Handle: brass cylinder scale `(0.016,0.05,0.016)`, local rot `(90,0,0)`, local z `0.05`
  - Lens rings: `Torus(tube 0.09)` mesh, radii `{0.085, 0.065, 0.045}` at local z `{0.18, 0.215, 0.245}`, material `NiscopeRing`
  - Lens Flash: quad scale 0.3 at z 0.245, `Glow_Lens`
  - Lens Light: point at z 0.295, `wattsonAccent`, range 1.8, intensity 0
  - Beam: `LineRenderer` local `(0,0,0.245)` -> `(0,0,2.4)`, width 0.035, `Glow_Beam`, hidden until SH11+0.45
  - `Niscope`: rings open over 0.6 s with 0.25 s stagger (`ease = open + sin(open*pi)*0.25`), counter-rotate at `120 + 70*i` deg/s, flash burst `exp(-3t)` after the last ring, `flarePeak 6`; `timeScale` and `WattsonController.motionScale` drop to 0.3 for 1.3 s on the SH08 "SlowMotion" signal.

---

## 7. VFX approach

`Particles(name, parent, position, material, setup)` base: renderer material + shadows off + `colorOverLifetime` alpha `0 -> 1@0.15 -> 1@0.6 -> 0`. Everything additive through `Glow`. Activation is via Timeline `ActivationTrack`s under a "VFX" `GroupTrack` with `PostPlaybackState.Inactive`; one-shot moments use `SignalTrack` + `SignalReceiver` into `Scene04Director`.

| Effect | Parameters |
|---|---|
| Charge Streams Blue/Gold (SH01) | at `Group + (-0.9,0.1,-0.9)`, non-loop 2.6 s, lifetime 1.2-2.4, speed 0.2-0.9, size 0.02-0.07, max 500, rate 160 x curve(1->0), sphere r 1.1, `velocityOverLifetime` orbitalY `+2.2` (blue) / `-1.7` (gold), orbitalX 0.6, radial -0.35, trails lifetime 0.25 width 1->0, trail material = same |
| Landing Dust | `(Group.x-0.3, FloorY+0.03, Group.z-0.5)`, burst 28, lifetime 1.2-2.2, speed 0.2-0.9, size 0.15-0.45, hemisphere r 0.5 rot (-90,0,0) |
| Dust Motes (always on) | `(-1.5,0.2,-1.3)`, box `(3.4,2.4,4.2)`, lifetime 6-11, speed 0.01, size 0.006-0.016, max 260, prewarm, rate 26, noise strength 0.03 freq 0.25 |
| Reticle (SH09) | at `Wire - view*0.05` facing `view = normalize(Wire-Hover)`; 48-point circle r 0.16 + 4 ticks 0.12-0.22, width 0.005, `Glow_Reticle`; `PopIn fromScale 2.6, overshoot 0.8, duration 0.5, pulse 0.04` |
| Wire Highlight | stencil outline duplicate (section 4) + Shimmer particles: box `(0.42,0.012,0.012)`, rate 70, lifetime 0.3-0.7, speed 0.03, size 0.008-0.022 |
| Dive Streaks (SH11) | at `Wire`, rotated `LookRotation(-view)`, cone angle 24 r 0.03, rate 320, lifetime 0.25-0.6, speed 4-9, size 0.01-0.03, max 600, renderer `Stretch`, velocityScale 0.12, lengthScale 2 |
| God Rays | see section 1 |

Line renderers (`Lines(...)`): `numCapVertices 2`, shadows off; copper leads width 0.004 with a sag midpoint.

---

## 8. UI theme

- Canvas: `ScreenSpaceOverlay`, sorting 50, `CanvasScaler` ref `1920x1080`, match `0.5`; `SafeAreaLayout` child; white fade image (`fadeSeconds 0.6`), dev note 26 pt `(0,0,0,0.45)`.
- `UI.Theme = ModulePalette`; panel sprite `Art/Interface/RoundedPanel.png` (sliced), knob `UI/Skin/Knob.psd`; font Nunito SDF.
- `UI.MinTarget = 88 px` (every button is at least 88x88). `UI.Button` default font 30, label insets 14/6, panel colour `palette.panel`.
- Controls (top-right, `CinematicControlsUI`): five buttons `150x88`, font 26, anchored `(-36 - slot*160, -36)`; volume panel `520x300` at `(-36,-136)`, labels 28, sliders 300 wide with 44 px knob, fill `wattsonAccent`, track `(1,1,1,0.25)`; auto-hide after 3 s; Skip needs a 1 s hold.
- Subtitles: box bottom-stretched, insets 210 L/R, 40..214 px up, bg `(0,0,0,0.62)`; speaker 26 pt `wattsonAccent` TopLeft; body 38 pt white, `Page` overflow (two lines per page, paged by VO progress).
- Ask Wattson button: `170x190` bottom-right `(-28,28)`; magnifier ring 140 px `wattsonAccent`, lens 120 px `(0.08,0.12,0.2)` masked with `WattsonFace.png` (256x256 portrait rendered at FOV 30 from 0.62 m); handle `22x60` rotated 45; caption "ASK WATTSON" 22 bold.
- Ask panel: `640x1016`, slides from `x = Width+60` to `-28` at rate `1-exp(-14 dt)`; title 34 bold accent; bubbles 26 pt, user tint `voltage` a 0.3, Wattson `wattsonAccent` a 0.16, padding 18/12, max 4; chips `88` tall `(1,1,1,0.14)` 28 pt; mic 96 px `(0.16,0.36,0.6)` -> listening `(0.9,0.3,0.25)`; input 28 pt; Back `420x88` 30 pt `(0.16,0.36,0.6)`; Stop `160x88` 28 pt.
- MCQ panel: width 1180 at y -130, question 36 pt, option rows 104, buttons 32 pt left-aligned, hint 28 pt in `palette.current`, shake 22 px / 0.45 s.
- Tap-the-wire icon: 150 px ring `(1,1,1,0.22)` + white finger/palm, pulses `1 + 0.12 sin(5t)`, auto-dive after 6 s.

---

## 9. Audio conventions

`Audio(name, parent, kind, gain)` -> non-spatial `AudioSource` + `AudioBus`. Music bus gain `0.35`, looping 16 s pad. SFX on two alternating Timeline lanes so neighbours never crossfade. Placeholder WAVs live in `Scene04/Audio/SFX` (Whoosh, Zip, Pop, Zap, Click, Chirp, Crackle, Shimmer, Beam, Dive, Pad) and `BlockoutAssets.Sfx()` already reads them.

---

## 10. Helpers to lift out of `Scene04Builder` (currently `private static`)

All asset-writing helpers hard-code `S4 + "/Materials"`, `S4 + "/VFX"`, `S4 + "/Prefabs"`; the shared versions need a folder parameter (or a `Shared/Art` folder for the module-wide ones).

**Asset / material helpers**
- `static T Asset<T>(string path, Func<T> create) where T : Object`
- `static Material Mat(string name, string shader, Action<Material> setup)`
- `static Material Lit(string name, Color color, float metallic, float smoothness, Color? emission = null)`
- `static Material Glow(string name, Color hdr, bool softTexture = true)`
- `static Material EmissiveCopy(Material source, string name, Color emission)`
- `static Color Hdr(Color color, float intensity)`
- `static Mesh MeshAsset(string name, Action<Mesh> fill)`
- `static Mesh Torus(string name, float tube)`
- `static GameObject Variant(GameObject instance, string name)`

**Scene-object helpers**
- `static LineRenderer Lines(string name, Transform parent, Material material, float width, bool loop, bool world, params Vector3[] points)` (missing from base)
- `static Transform Find(GameObject root, string name)` (base has a throw-less local lambda; lift the throwing one)
- `static AudioSource Audio(string name, Transform parent, AudioBus.Kind kind, float gain = 1f)` (base version lacks `gain`)
- `static ParticleSystem Particles(string name, Transform parent, Vector3 position, Material material, Action<ParticleSystem> setup)` — **name clash** with `SceneBuilderBase.Particles(string, Transform, Material, int)` (static carrier systems); rename the Scene 4 one e.g. `Emitter(...)`.
- `static GameObject Prim(PrimitiveType, string, Transform, Vector3 position, Vector3 scale, Material, Vector3 euler = default)` — base already has `Prim`, but Scene 4 sets **world** position/rotation while the base sets **local**; the apparatus code depends on world placement, so either lift as `PrimWorld` or convert the positions.

**Lighting / post**
- `static VolumeProfile Profile(string name, Action<VolumeProfile> setup)`
- `static T Override<T>(VolumeProfile profile) where T : VolumeComponent`
- `static VolumeProfile Lens(string name, float focalLength, float aperture, bool dive = false, bool motionBlur = false)`
- Local `Light Flame(string name, Vector3 position, float intensity, float range, bool shadows)` inside `BuildLighting` -> promote.
- `BuildLighting(Stage)` -> split into `StudyLighting(Transform root) : ReflectionProbe` (render settings + lights + probes + god rays) and `GlobalPostVolume(Transform root)`; the probe bake line in `Build()` goes with it.

**Cameras**
- `static CinemachineCamera Vcam(Stage s, Transform parent, string name, Vector3 position, Vector3 lookAt, float fov, Transform focus, VolumeProfile lens, Transform track = null, float noise = 0)` -> replace `Stage s` with the `cams` dictionary; it supersedes the base `Cam(...)` (which has no lens/focus/noise).
- Main-camera setup block in `BuildCameras` (SMAA, HDR, post on, Cut blend, sky background) -> `PolishedMainCamera(Vector3 position, Color background)`.
- Timeline local functions `Shot`, `Move`, `Sfx` (two-lane), `Active`, `Signal` and the overlap->blend loop -> helpers on the base `Timeline()` so beats can request a two-vcam "Move" shot.

**Content kits**
- `Word(Stage s, string text, Vector3 position, Color color, string sparkName)` -> `GlowWord(Transform parent, string text, Vector3 position, Color color, Vector3 faceFrom, string materialFolder)`; should back the blockout `Card(...)`.
- `BuildApparatus(Stage s)` -> `OhmApparatus(Transform root, Vector3 wire, Vector3 galvanometer, Vector3 pile, Vector3 notebook, float deskY)` returning the wire transform; reuse in Scenes 7-8.
- Lab dressing block in `BuildStage` -> `StudyLab(Scene scene) : GameObject` (instantiate variant, disable lights/clutter, flame/window shadow rules, static flags).
- Niscope block in `BuildWattson` -> `NiscopeGadget(WattsonController w, Color accent, Dictionary<string,GameObject> fx) : Niscope`; the base currently gives Wattson a placeholder cylinder.
- Wattson wiring block (rig lookups, glow material, follow light, spark trail) -> largely duplicated in `SceneBuilderBase.Wattson(...)` already; move the shared part to one place and add the Spark Trail + Follow Light that the base omits.
- `RenderPreviews` inner `Render(int width, int height, string file)` -> shared screenshot helper (`SingleCameraRequest`) for the blockout shot logs.

Already shared and fine to keep in the base: `Data<T>`, `Node`, `Line`/`L` dialogue builders, `Context`, `BuildSfx` output via `BlockoutAssets.Sfx()`, `Wav.Encode` (in `Shared/Agent/AgentServices.cs`).
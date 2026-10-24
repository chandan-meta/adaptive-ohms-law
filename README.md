# Adaptive Ohm's Law

An interactive 3D learning module for teaching voltage, current, resistance, graph interpretation and Ohm's Law. Wattson guides learners through predictions, laboratory equipment, circuit construction, measurements, graphing and equation-building activities.

[Play the current WebGL release](https://chandan-meta.github.io/adaptive-ohms-law/?v=20261024-fullscreen). The small button in the bottom-left corner enters or exits fullscreen when supported by the browser; Esc also exits.

## Requirements

- Unity 6000.3.0f1
- Universal Render Pipeline
- WebGL browser, Windows or Android tablet target
- Landscape orientation

## Open and run

1. Add this folder in Unity Hub.
2. Open it with Unity 6000.3.0f1.
3. Open `Assets/OhmsLaw/Scenes/Scene00_ModulePlayer.unity`.
4. Enter Play mode.

The current module plays Scenes 04–13, including the study, wire interior, laboratory, graph and free experiment. The editor's module player supports jumping between scenes; release builds open on the lesson's title screen.

## Build and publish

Use **Ohm's Law → Build WebGL** in Unity. If it switches the target platform, run the menu item again after importing finishes. The build is written to `Builds/WebGL` and its result to `Logs/WebGL/build.txt`.

After a successful build, `bash Tools/publish_webgl.sh` publishes it to the existing GitHub Pages deployment branch without changing source files or the staged index. See `Docs/Optimization/WebGL_2026-10-24.md` for the current build verification and `Docs/UI_v2/UNITY_FIGMA_INTEGRATION.md` for the directly exported Figma artwork.

Run `node Tools/test_webgl_template.cjs` to check fullscreen availability, loading state, enter/exit behavior and accessible labels. Release builds use Unity's Disk Size optimization alongside Brotli compression.

## Main features

- Adaptive concept mastery and local learner profiles
- Data-driven questions and dialogue
- Animated Wattson companion with contextual chat
- Voice input/output hooks and subtitles
- Editable 3D laboratory equipment and circuit terminals
- Rotary voltage control, camera joystick and reset view
- Interactive voltage-current graph and resistance activities
- Responsive desktop and landscape tablet UI
- WebGL build and local backend scripts

## Local backend

Backend credentials are deliberately excluded from version control. See `Backend/README.md` for local setup. Never commit API keys or files from `Backend/Local`.

## Project structure

- `Assets/OhmsLaw/Scenes` — current lesson scenes
- `Assets/OhmsLaw/Shared` — current UI, Wattson, circuit and lesson systems
- `Assets/OhmsLaw/Shared/Resources/FigmaUI` — Figma PNG exports and licensed fonts
- `Assets/AdaptiveLearning` — earlier implementation and shared imported art
- `Assets/AdaptiveLearning/Prefabs` — reusable equipment and character prefabs
- `Assets/AdaptiveLearning/Documentation` — implementation and asset notes
- `Backend` — local chat and WebGL hosting utilities

## Asset notices

Font licences and source notes are stored under `Assets/AdaptiveLearning/Art/Interface/Fonts`. Review the documentation and source/licence notes for imported 3D assets before redistributing a public build.


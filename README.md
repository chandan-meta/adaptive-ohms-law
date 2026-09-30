# Adaptive Ohm's Law

An interactive 3D learning module for teaching voltage, current, resistance, graph interpretation and Ohm's Law. Wattson guides learners through predictions, laboratory equipment, circuit construction, measurements, graphing and equation-building activities.

## Requirements

- Unity 6000.3.0f1
- Universal Render Pipeline
- Windows or Android tablet target
- Landscape orientation

## Open and run

1. Add this folder in Unity Hub.
2. Open it with Unity 6000.3.0f1.
3. Open `Assets/AdaptiveLearning/Scenes/Scene_06_AdaptivePrediction.unity`.
4. Enter Play mode.

The implemented learning sequence continues through Scenes 8–13. Scene 7 is represented by the preceding cinematic/storyboard handoff.

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

- `Assets/AdaptiveLearning/Scenes` — lesson scenes
- `Assets/AdaptiveLearning/Scripts` — learning, UI, Wattson, circuit and save systems
- `Assets/AdaptiveLearning/Prefabs` — reusable equipment and character prefabs
- `Assets/AdaptiveLearning/Documentation` — implementation and asset notes
- `Backend` — local chat and WebGL hosting utilities

## Asset notices

Font licences and source notes are stored under `Assets/AdaptiveLearning/Art/Interface/Fonts`. Review the documentation and source/licence notes for imported 3D assets before redistributing a public build.


# UI v2 handoff — 12 October 2026

Design checkpoint: editable Figma work created, Unity integration awaiting user approval.

Latest correction: read `LUMINOUS_HANDOFF.md` and `polish-state.json` first. Node 5:2489 and its shared component mains have been visibly restyled with editable gradients/halos/reflections. Review comparison: node 37:2. Compact component index: node 3:597. Native RGBA decorative exports are in `FX/`; Unity integration still requires the user's corrected-design approval.

See `README.md` for Figma/component/prototype links, exported assets, reference deviations and validation limits. Read `figma-state.json` before continuing Figma work, and re-inspect the relevant page to account for any user edits.

Workspace facts: Unity 6000.3.0f1, URP 17.3.0, uGUI 2.0, new Input System 1.16.0, landscape Windows/Android plus WebGL infrastructure. Both OhmsLaw and AdaptiveLearning lesson systems remain in Build Settings. Runtime-generated shared HUD, serialized scene UI, hidden prefab content, world-space labels and chat must all be migrated. Inventory statuses accurately remain pre-integration.

No Unity scenes, prefabs, gameplay scripts or ProjectSettings were changed in this phase. Previously untracked export tools/assets were left in place. New work is staged under `Docs/UI_v2`.

The learner UI must preserve Wattson, the environment, narration, curriculum, all existing input/event gates and saved profiles. Current shared lesson data is five readings at 5 Ω; old prose is inconsistent. Do not silently alter curriculum based on those notes.

After design approval: take a recoverable VCS checkpoint, inspect any newer working-tree changes, integrate shared components into existing uGUI, migrate every inventory row, and execute the three validation runs. Keep legacy assets until replacements pass. Do not rebuild hand-edited scenes from older builders.

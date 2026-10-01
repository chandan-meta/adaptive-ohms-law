# User's master prompt — visual upgrade for Scenes 5 to 13 (verbatim essentials)

The uploaded reference images (Docs/Polish/References/) are the FINAL VISUAL TARGET, not inspiration. Modify the existing Unity
project; do not rebuild from scratch, do not redesign the educational content, do not replace working gameplay systems.

Scope: Scenes 5–13 only (Scenes 1–4 only via shared prefabs/UI that must be safely updated). Every scene inspected, modified,
tested, visually validated. Preserve: lesson progression, dialogue, VO, subtitle timing, animations, transitions, experiment and
scientific logic, interactions, adaptive learning, quizzes, Pause/Replay/CC/Volume/Skip, Ask Wattson, progression.

Target style: POLISHED STYLIZED 3D EDUCATIONAL GAME (not prototype, not debug, not giant floating text, not default Unity, not
photoreal). Environment: polished science classroom/laboratory — warm classroom lighting, dark green/teal teaching boards,
blue/navy cabinets, wooden or clean lab work surfaces, posters, books, notebooks, small props, plants, soft contact shadows,
subtle reflections, depth, controlled bloom.

Camera: match each scene's reference; no huge near objects, no text over faces, no hidden equipment, no cut-off heads, no stray
feet, no equipment outside the frame, no distortion. Apparatus scenes frame DC SOURCE → AMMETER → X → TEST WIRE → Y → SWITCH.
Lecture scenes: scientist and Wattson visible, table and board readable, audience silhouettes only when appropriate, UI never
blocks faces or important objects.

HUD (top-right): PAUSE REPLAY CC ON/OFF VOLUME SKIP — dark navy/charcoal, rounded, white uppercase, equal size/spacing, subtle
transparency, hover/pressed. Canvas 1920×1080 scale-with-screen-size; test 1920×1080, 1600×900, 1366×768.
Subtitles: wide dark translucent rounded panel, speaker name upper-left (WATTSON in cyan), white body, 2–3 lines, never clipped,
never over the Wattson UI. Ask Wattson: bottom-right circular avatar, cyan glow, magnifier, "ASK / WATTSON".

Callouts: remove giant floating world text; one reusable ObjectCallout (dark navy panel, thin orange border, orange title, white
description, thin orange leader line, small circular marker), screen-space tracking a world target; no overlap, no clipping, no
duplicates, no text larger than the equipment. VFX callouts: compact magenta panels ("[VFX] galvanometer needle swing",
"[VFX] Pulse travels from X to Y") that never cover the gameplay area. Concept panels: dark blue, cyan edge, orange title,
white body, simple cyan diagrams, short text. Teaching board used deliberately (Current → I, Voltage → V, relationship question,
investigation, graph, circuit diagram) — no random floating text.

Experiment table: DC source, ammeter, X, nichrome wire, Y, switch, red and black cables; scale, spacing, materials, contact
shadows, cable routing, metallic terminals, ammeter face; nothing floats. George Ohm: intentional natural poses (presenting,
examining wire, gesturing, at the galvanometer); never T-pose, never clipped, face visible. Galvanometer: hero close-up (glass
cylinder, metal base, scale, needle) with a visible needle swing. X → Y pulse: magenta/pink glowing pulse with trail and chevrons
along the actual wire. Electrons: dark-blue immersive space, cyan/blue spheres with clear minus signs, depth layers, controlled
count, directional motion, trails, bloom; never covering Wattson or subtitles. Battery: − blue/cyan left, + red/orange right, a
clear conductor path between them with electrons moving through.

Materials: copper, nichrome, metal, glass, plastic, rubber, wood, paper, cloth clearly different; no default materials.
Lighting: warm key, soft fill, subtle rim, soft/contact shadows, balanced exposure; keep the render pipeline. Post: light bloom,
mild grading, AO, subtle vignette, mild DoF; no heavy effects; UI perfectly readable. Typography: TextMeshPro hierarchy (lesson
title, concept title, callout title, body, subtitle), proper font sizes, no transform scaling.

Fix all visual bugs: giant/duplicated/overlapping/clipped/unreadable text, wrong-facing canvases, text in geometry, sorting,
z-fighting, clipping, floating props, duplicate meshes, T-poses, missing materials, scale, near clipping, stray body parts,
objects outside frame, debug text, unused placeholders. Reusable prefabs where practical: GameplayHUD, SubtitlePanel,
AskWattsonButton, ObjectCallout, VFXCallout, ConceptPanel, LessonBoard. Do not break serialized references.

Three dry runs after implementation: (1) UI only, Scenes 5→13, fix and re-run; (2) gameplay visuals vs references, fix and
re-run; (3) complete student experience 5→13, "would any part look unfinished?", fix and re-run. Then one representative Game View
screenshot per scene compared with its reference. Performance: no excessive lights/particles/textures/transparency. Code quality:
clear names, cached references, no needless Update/Find, null checks, inspector fields, fits the architecture. Safety: reversible,
inspect references before destructive edits. Final acceptance checklist as in the prompt. After completing, upload to git.

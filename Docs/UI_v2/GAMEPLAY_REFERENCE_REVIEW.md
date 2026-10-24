# Gameplay reference review — 23 October 2026

25 separate 1920×1080 editable Figma gameplay studies recreate the supplied capture sequence in the navy blue and cyan theme. Native text, shared component instances, tables, graph geometry, formulas, slider shapes, response choices and feedback remain editable. Existing pages were preserved.

[Open the 25-screen index](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq/OhmsLaw_UI_v2?node-id=74-1839). Page: 06 — Gameplay References · Navy Cyan. Each numbered card navigates to its screen. Header arrows advance through the study; question, clue, replay and exploration controls have selected prototype connections.

The source is Reports/Revision_2026-10-22/Snapshots/01–25. Complete screenshots were used only as layout and content references, never imported into the output. The original older export previews have now been replaced with fresh native Unity captures of the current environments in LatestEnvironment/. These show the wooden classroom, historical study and metal wire chamber. The captures sample current scene cameras and poses; they are not exact replays of each supplied screenshot's animation time or transient VFX. Native particles in the copper/nichrome comparison are a simplified conceptual illustration, not a new simulation.

Scientific mapping: voltage is vertical and current horizontal; sample readings are 1–5 V and 0.20–1.00 A; V/I = 5 Ω. The slope annotation spans ΔV = 2 V and ΔI = 0.40 A. The free-experiment study distinguishes saved 3 V / 0.60 A at 5 Ω from live 1.5 V / 0.15 A at 10 Ω. Selected rows have outlines and check marks. Circuit symbols explicitly identify A in series and V in parallel.

Verification: all 25 screen bounds checked with no visible text beyond the frame. Representative title, circuit, quiz/hint, experiment, material comparison, linked-data, slope, clue-builder, law, completion, exploration and index renders were visually reviewed. Graph tick labels intentionally extend four pixels beyond the unclipped inner plot but stay inside the outer graph panel. Prototype connections are review navigation; sliders and calculations do not run a Unity simulation.

Final state ledger: gameplay-reference-final.json. Construction provenance: gameplay-reference-build.js; later corrective changes are recorded in the final ledger. Do not rerun the construction file blindly: it contains range placeholders and skips populated frames.

The temporary renderer helper was removed from Assets after capture. No gameplay or scene assets were edited or saved. Fresh UI-free captures are complete; runtime UI integration and device testing remain pending. The environment correction also updates 27 existing desktop backgrounds, 29 tablet backgrounds and the after-comparison image. See latest-environment-state.json for the exact nodes and image hashes.

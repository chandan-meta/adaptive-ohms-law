# Run 3 — performance and regression

**Not executed during the design phase.** No player build or physical device was profiled for the redesign.

After approval/integration, record Unity/player version, build target, resolution and actual hardware. Compare frame time and UI rebuilds, check missing assets/errors, duplicate EventSystems, input blocking, event leaks, repeated audio and transition/pause behaviour. Test native touch and microphone paths where available. Use unscaled UI animation and the solid-panel fallback. Never mark unavailable device checks as passed.

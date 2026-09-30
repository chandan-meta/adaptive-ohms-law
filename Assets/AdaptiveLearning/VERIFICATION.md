# Scenes 06–08 verification — 28 September 2026

Unity 6000.3.0f1, URP, existing Input System. Tests use isolated in-memory profiles and JSON under Logs; the learner's persistent profile was not reset.

## Current results

- Revised Scene 7 rotary Play-mode suite: **69 passed assertions**, no runtime errors. The earlier circuit/scoring suite also passed 60 assertions before the rotary revision.
- Final continuous Scene 6 → 7 → 8 suite: **298 passed assertions**, no runtime errors.
- Final graph rendering/interaction suite: **238 passed assertions**, no runtime errors.

Coverage includes actual scene loads and fade handoffs, retained Scene 6 prediction recall, the fixed-resistance circuit, open-switch recording prevention, sorted observations collected out of order, saved unfinished experiment sessions, Scene 7→8 data provenance, saved unfinished plotting, wrong-answer retries, guided completion, help without penalty, replay without mastery inflation, sample-data practice without assessed mastery, completion events, and JSON roundtrip. The focused graph suite also checks the generated grid/line geometry and actual mouse access to the modal Close button above the 3D viewport.

The rotary revision also verifies dragging with mouse and touch, recording after a drag, 0–3 V endpoints, left/right keyboard and scroll fine adjustment, intermediate-setting recording prevention, chat interaction gating, physical ammeter agreement, and dial touch bounds. Tests retain the same simulated devices between gestures to represent a continuous input session.

Input tests inject mouse and touchscreen device events through Unity's Input System and UI raycasting. These are simulated touch tests, not physical tablet tests.

UI reviewed at **1920×1080, 1920×1200, 1280×800, 2560×1600 and 1440×1080**. Active controls were checked for screen bounds and minimum 44-pixel touch height at these sizes. Desktop and tablet screenshots were inspected, including the shared conductor setting, compact dialogue/controls, rotary apparatus, graph plotting, completed pattern and chat. The latest Scene 8 composition uses an opaque graph, subtle point markers with 68-unit hit areas, a separate companion/reference-circuit column, and no overlapping apparatus labels. Plotting, completed-pattern and chat screenshots were inspected at desktop and tablet ratios after this change. The full graph suite was rerun successfully. Visual review caught and corrected missing graph mesh rendering and URP camera drawing above the chat modal.

Evidence:

- `Logs/Rotary-validation.txt`
- `Logs/Scene07-rotary-*.png`
- `Logs/Scene06-immersive-*.png`
- `Logs/Scene07-validation.txt`
- `Logs/Lesson-continuity-validation.txt`
- `Logs/Scene08-review-validation.txt`
- `Logs/Lesson-test-profile.json`
- `Logs/Scene07-reviewed-*.png`
- `Logs/Scene08-reviewed-plotting-*.png`
- `Logs/Scene08-reviewed-pattern-*.png`
- `Logs/Scene08-reviewed-chat.png`
- `Logs/Scene08-clean-layout.png`

Run the verification from the Adaptive Learning editor menu. Keep Unity unminimized while testing Game view resolutions. Historical failed/diagnostic attempts in Editor.log are superseded by the successful result files above.

## Remaining acceptance work

Physical Android tablet performance, touch hardware and soft keyboard behaviour have not been tested. The final voiceovers, character and apparatus art are placeholders. Chat is a local contextual provider, not an external AI service. Scene 8 deliberately ends at its completion event; no Scene 9 content was built.

Earlier Scene 6 verification remains available in `Logs/Scene06-validation.txt` (169 assertions) and its earlier Windows build summary. The current full-lesson Windows build result is recorded separately in `Logs/Scenes06-08-build.txt`.

## Current Windows build

The continuous Scenes 06–08 Windows x64 development build succeeded with **0 errors and 0 warnings**. Output: `Builds/Scenes06-08/AdaptiveOhmsLaw.exe`. The editor Play-mode tests above verify behaviour; a separate standalone-player walkthrough and physical Android acceptance remain outstanding.



## Wattson conversation verification — 28 September 2026

The final chat suite passed 33 assertions, including a real local HTTP request/JSON response, contextual conversation history, cancellation, failure recovery, scrolling, five landscape resolutions and history across scene loads. No runtime errors occurred in that suite. Screenshots were inspected after making the chat card opaque so lesson text cannot show through. Results: Logs/Wattson-chat-validation.txt. This verifies the client integration; no live AI provider or speech service was tested.


After the chat changes, the complete Scenes 06–08 regression passed 308 assertions with no runtime errors (18:42, 28 September 2026). The Windows development build then succeeded with 0 errors and 0 warnings (18:43). Result files: Logs/Lesson-continuity-validation.txt and Logs/Scenes06-08-build.txt. Physical Android and live AI/voice acceptance remain outstanding.


## Voice integration — 29 September 2026

All 37 scripted lines across Scenes 6–8 now reference generated English WAV clips. This includes introductions, all three assessment prompts, hints and feedback. Dialogue durations account for clip length; handoffs wait for the current line. Opening chat pauses lesson audio and closing chat resumes it.

The updated conversation suite passed 41 checks, including real Windows device synthesis played through Unity with nonzero audio output, microphone-button availability and touch sizes at five resolutions. Android native speech bridge source compiled successfully against Android 35 and Unity's player classes. No actual microphone transcription or physical Android run was performed; those require a learner's microphone/service permissions and hardware acceptance. No live generative AI backend is configured.


With voice enabled, the complete lesson regression passed all 308 checks with no runtime errors. The final refreshed Windows build succeeded at 10:26 on 29 September 2026 with 0 errors and 0 warnings. Output: Builds/Scenes06-08/AdaptiveOhmsLaw.exe. The earlier incremental build warning did not recur in the refreshed build; the final build-message report is empty.


## Microphone recovery and AI connection — 29 September 2026

The original error was Windows speech privacy HRESULT 0x80045509, not a missing device. After the user enabled permissions, Unity dictation still stayed stopped. Windows voice input now uses its installed offline English System.Speech recognizer through a hidden command process, with temporary result cleanup. The new microphone smoke test detected two devices and reached Running/Listening; a recorded speech sample was transcribed correctly. An actual learner utterance in the room remains a manual acceptance check.

Chat validation passed 43 checks including accurate permission diagnostics and audio output. The Google credential was accepted by Google's model-list API, and is Windows-DPAPI encrypted under Backend/Local, outside all Unity assets. The local proxy passed request/context and HTTP error tests. Live Google generation repeatedly returned 503 high demand; no successful live AI response is claimed. Figma remained unconnected at the last capability check.


Final shared-theme lesson regression passed 308 checks with no runtime errors after isolating simulated input from desktop focus changes. The Windows build at 11:10 on 29 September 2026 succeeded with 0 errors and 0 warnings. The chat suite passed 44 checks, including explicit capacity-error fallback. The spoken test phrase 'What is voltage?' was correctly transcribed after adding common lesson phrases alongside free-form dictation. Full live microphone conversation in the learner's room and Android device acceptance remain manual checks.

Windows now targets the running loopback Google backend. A stateless Interactions API probe also returned 503 high demand, confirming that an alternate Google API path did not resolve provider availability. No successful generated response is claimed. The backend and credential are outside the Unity build; tablets still need a hosted HTTPS endpoint. Figma remains unconnected; current UI changes were implemented directly in Unity, not from a Figma file.


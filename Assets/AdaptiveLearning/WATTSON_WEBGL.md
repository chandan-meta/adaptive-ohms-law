# Wattson model and local browser build

The supplied `LB_FK.fbx` is imported under `Art/Wattson/LB`. Source: the user's `Received Files/LB_Rig` folder. No external model was downloaded. Redistribution rights are not supplied with the FBX.

The rig has no animation clips or accompanying texture files. The saved visual uses URP materials and a relaxed arm pose. Existing companion movement, turning, dialogue and voice remain on `WattsonController`. This is not a finished facial/lip-sync animation set.

Edit `Prefabs/Wattson/Wattson.prefab`, or expand `Wattson LB Visual` on the character in a lesson scene. The original visual is retained as a disabled child. The FBX child preserves its editable bone hierarchy. Keep the wrapper because the controller animates it separately from the model's scale and pose.

## Browser build

Use **Adaptive Learning > Build Complete Web Lesson**. Unity 6000.3.0f1 needs its Web Build Support module. Output: `Builds/WebGL`. The build includes Scene 06 first, then Scenes 08–13. Earlier cinematic video scenes are not implemented in this project; SampleScene is an empty template and is excluded.

Run `node Backend/serve-webgl.mjs` from the project folder and open `http://localhost:8080`. The server binds only to this computer. It serves uncompressed WebGL files with the correct WebAssembly MIME type and proxies chat to the existing loopback backend on port 8787. Start that backend separately using its existing instructions. Provider credentials stay on the backend.

Browser progress uses PlayerPrefs/IndexedDB and is separate from desktop JSON progress. Clearing site data clears browser progress.

Alternatively, run `Backend/Start-WebLesson.ps1` to start both services after a restart. Click **Start lesson** on the landing page. Verification: zero build errors/warnings; browser Scene 06 incorrect answer, hint, retry, correct answer, mastery change and transition into Scene 08 passed. No browser runtime errors appeared. The live Google backend returned HTTP 429 (`provider_quota`); external AI replies require available quota. Actual microphone audio capture has not been tested. The current server session may show Unity's non-fatal persistent-data synchronization deprecation warning; the server source opts into automatic synchronization on its next restart.

Browser narration uses installed browser speech voices. Microphone input uses browser speech recognition when available; Chrome/Edge are the intended browsers. Permission, network connectivity, browser support and an actual working microphone are required. Unsupported input shows a message and typing remains available. Browser policies may require a user click before audio can start.

Windows and Android retain their existing voice implementations. No provider credential is embedded in the browser build.

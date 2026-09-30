# Wattson and table exploration

Scenes 8 and 9 now have a small left-side joystick. Drag with a mouse or touch to orbit the table; use WASD or arrow keys on desktop. Vertical movement changes the viewing elevation. The camera stays within a bounded front/side view of the bench, pulling back slightly for side views. RESET VIEW or Home restores the lesson composition. Camera movement is suspended while Ask Wattson is open, and world-space labels turn toward the learner.

The microphone button shows a white microphone and HOLD TO TALK. Hold to capture; release to transcribe and send. The icon turns red while listening. Existing microphone selection and live-level monitoring remain available.

The connected chatbot accepts general questions, carries conversation and lesson context, and enables Google Search. Grounded responses expose a Google Search sources button; source pages include the provider's search suggestions. Assessment decisions remain in Unity. Provider errors preserve the question for retry and are no longer replaced by an unrelated built-in response.

Live Google requests currently return provider quota/capacity errors. Backend implementation tests pass, but successful real online answers still require provider availability. Android also requires the previously documented hosted HTTPS endpoint; the Windows loopback service cannot be reached as localhost from a tablet.

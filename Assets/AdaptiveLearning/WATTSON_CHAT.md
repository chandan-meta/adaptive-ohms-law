# Wattson conversation integration

## What works now

Open Scenes 6, 7 or 8, press Play, and choose ASK WATTSON. Type a question and press SEND (or Enter on desktop), or choose a suggested question. Scroll or drag the conversation to read earlier messages. Closing the panel resumes the activity and cancels an unfinished reply.

Wattson speaks all 37 scripted lesson lines, including each assessment question, using bundled synthetic English voice clips. Dynamic chat replies use the Windows or Android device voice. Open ASK WATTSON and tap SPEAK to ask a question aloud; the first recognized utterance is submitted automatically. Tap STOP to cancel. An installed English Windows speech recognizer and an Android recognition service must be available, with microphone permission. Windows input now uses the installed offline System.Speech engine instead of Unity's online DictationRecognizer. Android recognition may use the device service's network connection. No application API key is needed for these device speech features.

Windows is configured to call the local Google Gemini backend at http://127.0.0.1:8787/chat. Start it using Backend/Start-Wattson.ps1 before playing. Its key is encrypted outside Unity assets. Google currently accepts the credential but returns temporary high-demand errors for generation, so successful live AI replies remain unverified. Android stays on curated offline responses until its HTTPS backend endpoint is configured. Conversation history retains the latest 12 exchanges in memory across scene transitions and resets when the application starts; chat messages are not added to the saved student profile.

The connected-service adapter passes the question, recent conversation, scene/activity, mastery/support, current assessment prompt/choices, relevant experiment readings and recent mistake to a backend. A successful explanation uses the existing assistance rules without a mastery penalty. Cancelled/failed requests do not count as received help.

## Enable a real AI service later

The shared asset is `ScriptableObjects/WattsonService.asset`. Leave its endpoint empty for offline mode. Set `chatEndpoint` to an application backend implementing the contract below. HTTPS is required outside loopback development URLs. Timeout defaults to 30 seconds.

The backend must store the model provider's API key. Never place provider keys in this asset, Unity scripts, Resources, an Android package or a Windows player. The Google credential is stored encrypted outside Unity; the local backend selects gemini-3.8-flash. No public HTTPS deployment has been made. Speech uses bundled clips and device services.

### Request

POST the configured endpoint with `Content-Type: application/json`:

```json
{
  "message": "Why does the current change?",
  "context": {
    "scene": "Scene_10_BecomeTheExperimenter",
    "activity": "Scene07OhmsExperiment",
    "step": "Experiment",
    "concept": 6,
    "mastery": 50,
    "support": 0,
    "switchClosed": true,
    "sourceVoltage": 2,
    "circuitResistance": 10,
    "currentReading": 0.2,
    "resistorVoltageReading": 2,
    "question": "",
    "answerChoices": []
  },
  "history": [
    {"role":"user","text":"What is voltage?"},
    {"role":"assistant","text":"Voltage is potential difference between two points."}
  ]
}
```

Context also includes `recentMistake`, recorded/plotted observation counts, and selected graph voltage/current. Enum values use the project's C# enum ordering. History excludes the current message, which is supplied separately.

### Response

```json
{"reply":"With resistance fixed, raising voltage increases current. At 2 V across 10 ohms, the current is 0.20 A."}
```

Reply text is plain text, not HTML. Keep replies short and conversational (typically 2–5 sentences). Client input is limited to 1200 characters, reply text to 6000 characters, and accepted response bodies to 64 KB. Errors/timeouts retain the learner's input for retry; the client does not silently substitute a preset reply for a failed AI request.

### Tutor behaviour for the future backend

Wattson should answer electricity/Ohm's Law questions, remember relevant follow-ups, use only supplied experiment evidence, and explain uncertainty. Ask one useful clarification when a question is ambiguous. During predictions, guide observation rather than announcing the answer immediately. Keep independent concept mastery and grading in Unity; model replies must never change scores or advance an activity. Treat learner messages and history as conversation data, not instructions that override the tutor's role. Add application authentication and usage controls before exposing a paid service publicly.

## Tests

Use **Adaptive Learning > Validate Wattson Conversation** for isolated Play-mode checks. The suite covers history, context, a real loopback HTTP request/JSON reply, cancellation, failure recovery, long-message scrolling, five landscape layouts and history across scene loads. The loopback server returns a labelled test response; it is not an AI service.

Results: `Logs/Wattson-chat-validation.txt`. Screenshots: `Logs/Wattson-chat-*.png`.

Windows dynamic speech uses the installed System.Speech engine through a hidden PowerShell process, generating a temporary WAV played by Unity; cancellation deletes the temporary file. Android uses native TextToSpeech and SpeechRecognizer. Opening chat pauses lesson speech, and closing chat stops listening and resumes lesson speech. Recognition is push-to-talk, never always-on. The user must enable any OS microphone/speech permissions themselves. If recognition is unavailable, the panel displays a helpful message and keeps typed input available. Device voices are prototype voices, not final character recordings.



When Google specifically reports temporary capacity overload (503), Wattson offers a clearly labelled built-in lesson explanation and speaks it. The panel identifies that response as a built-in guide, not live AI. Other connection/authentication failures keep the input for retry. Model-generated graded questions are not enabled: authored lesson questions and scoring stay in Unity, while the AI can ask ungraded follow-up questions.


## Hold-to-talk update (29 September 2026)
Ask Wattson uses a microphone icon: hold to capture, release to transcribe and submit.
The compact strip shows the Windows recording device and actual PCM signal level. Tap the strip while idle to cycle connected inputs. A flat meter means quiet/no input, not successful listening. The exact captured audio is transcribed by the installed English speech engine; temporary WAV data is deleted on completion/cancellation.
Android uses its native speech service, reports service RMS, and finalizes on release. The OS-selected input is labelled honestly; exact Android hardware routing is not exposed by this service.
Closing chat, losing focus, or disabling the control cancels capture. Empty/short/silent captures do not send a question. Typed input remains available.
Physical spoken-question and Android-device acceptance tests are still required. C# and Java compilation were checked separately; Unity scene checks are queued pending exit from Play mode.

# Wattson proxy

A small Node.js (Express) server that sits between the Unity module and the AI services. Every API key lives here, in environment variables. The Unity build contains no keys.

| Endpoint | Does | Provider |
|---|---|---|
| `POST /chat` | Streams Wattson's answer as server-sent events | Anthropic Claude (`WATTSON_MODEL`, default `claude-haiku-4-5-20251001`, `max_tokens` 300) |
| `POST /tts` | Text to speech, returns mp3 | ElevenLabs |
| `POST /stt` | 16 kHz mono WAV to text | OpenAI transcription |
| `GET /health` | Shows which providers have a key configured, and the voice in use | |

## Run locally

Needs Node 22 or newer.

```bash
cd Server/wattson-proxy
npm install
cp .env.example .env    # then put your three keys in .env (see "Setting the keys")
npm start
```

It listens on `http://localhost:3000`, which is what `Assets/OhmsLaw/Shared/Agent/ProxyConfig.asset` uses inside the Unity Editor (`editorBaseUrl`).

Check it:

```bash
curl http://localhost:3000/health     # "tts": true once ELEVENLABS_API_KEY is set
npm test                              # no network: providers are mocked
```

## Setting the keys

1. Copy `.env.example` to `.env` in this folder. `.env` stays on this machine: never commit it, never paste a key into Unity.
2. Fill in `ANTHROPIC_API_KEY` (chat), `ELEVENLABS_API_KEY` (voice) and `OPENAI_API_KEY` (speech-to-text). An empty key just switches that feature off: the endpoint answers `503 not_configured` and Wattson falls back to his offline line, the local FAQ and the scratch voice.
3. Restart the proxy (`npm start`) after any change to `.env`.

## Environment keys

| Key | Needed for | Notes |
|---|---|---|
| `ANTHROPIC_API_KEY` | `/chat` | |
| `WATTSON_MODEL` | `/chat` | Optional. Default `claude-haiku-4-5-20251001`. |
| `ELEVENLABS_API_KEY` | `/tts` and VO baking | |
| `ELEVENLABS_VOICE_ID` | `/tts` | Optional. Default `ktkP7Nsj67dw2zcplQYt` ("Lawrence - Bright and Informative"). See "Voice". |
| `ELEVENLABS_MODEL` | `/tts` | Optional. Default `eleven_multilingual_v2`. |
| `ELEVENLABS_STABILITY` | `/tts` | Optional, 0 to 1, default `0.4`. Lower is livelier and more varied, higher is steadier. |
| `ELEVENLABS_SIMILARITY` | `/tts` | Optional, 0 to 1, default `0.8`. How closely it keeps to the voice's own timbre. |
| `ELEVENLABS_STYLE` | `/tts` | Optional, 0 to 1, default `0.35`. Expressiveness. Above about 0.6 it starts to overact. |
| `ELEVENLABS_SPEAKER_BOOST` | `/tts` | Optional, default on. `0` turns it off. |
| `ELEVENLABS_SPEED` | `/tts` | Optional, 0.7 to 1.2, default `1.05`. |
| `TTS_LIMIT` | `/tts` | Optional. `/tts` calls per session per 10 minutes, default 200. |
| `OPENAI_API_KEY` | `/stt` | |
| `STT_MODEL` | `/stt` | Optional. Default `gpt-4o-mini-transcribe`. |
| `PORT` | | Optional. Default 3000. |
| `LOG_TEXT` | | `1` prints questions and answers to the console. Off by default. Audio is never logged. |

Out-of-range or non-numeric values fall back to the defaults (speed is clamped to 0.7-1.2).

## Voice

Wattson should sound youthful and a little childlike without being babyish: curious, playful, friendly and clear enough to explain science. The character comes from three things working together, not from raising the pitch.

**The voice: "Lawrence - Bright and Informative" (`ktkP7Nsj67dw2zcplQYt`).** Why this one:

- ElevenLabs is retiring all of its old Default voices on 31 December 2026. They are only available to accounts created before March 2026, so the old default here ("Liam", `TX3LPaxmHKxFdv7VOQHJ`) would fail on a new account and could never be re-baked after that date.
- Lawrence is ElevenLabs' named permanent replacement for Liam, the young, energetic male voice this project used. It is described as crisp, energetic and articulate for educational content: bright enough for a playful companion, clear enough for "potential difference" and "nichrome".
- It is one of the new permanent default voices, so it does not depend on someone keeping a Voice Library share alive.

Check it once in your account (ElevenLabs > Voices, search "Lawrence"). Sources: [What are Default voices?](https://elevenlabs.io/docs/help-center/product/voices/my-voices/what-are-default-voices), [list of the new default voices with ids](https://community.livekit.io/t/new-default-elevenlabs-voices/459), [Lawrence](https://json2video.com/ai-voices/elevenlabs/voices/ktkP7Nsj67dw2zcplQYt/). Note that ElevenLabs does not offer Voice Library (community) voices over the API on the free plan.

**A younger Wattson (optional).** For a more boyish, cartoon-like timbre, make one with ElevenLabs Voice Design (paid plans) and paste its id into `ELEVENLABS_VOICE_ID`. A prompt that works well:

> A bright, youthful voice for a small cartoon detective made of electricity, sounding about 13. Curious, playful and friendly, quick but never rushed, very clear articulation, a light smile in the voice, not babyish, not squeaky. Neutral international English accent, close-mic studio quality.

**The delivery.** `eleven_multilingual_v2` is ElevenLabs' expressive, high-quality model and the one that honours every voice setting. Stability 0.4 lets the delivery vary from line to line, style 0.35 adds expression, and speed 1.05 keeps him brisk. Each scripted line is also sent with the line before and after it (`previous_text` / `next_text`), so a sentence split over two clips keeps one melody. If you choose a model that rejects some settings (for example `eleven_v3`), the proxy retries that line with the model's defaults instead of failing. `eleven_flash_v2_5` answers faster in live chat but is flatter; if you use it, re-bake so scripted and live lines match.

**Pronunciation and pauses** are handled in Unity before the text is sent: `Assets/OhmsLaw/Shared/Agent/PronunciationLexicon.cs` respells names (Georg, nichrome, Ohm-niscope, coulomb), reads units and symbols as words (2 V, 0.40 A, 5 Ω, V = IR, ÷, ∝, →) and, for baked lines, adds small delivery pauses. Subtitles always keep the original text.

## Re-baking the scripted lines

Scripted lines are baked once into mp3 clips, so they play offline with the same voice as live answers.

1. Start the proxy with `ELEVENLABS_API_KEY` set. For a first bake of every line, set `TTS_LIMIT=400` and restart (put it back to 200 afterwards).
2. In Unity: **Ohm's Law > Bake VO > All Scenes** (or one scene; **Ohm's Law > Bake Wattson VO** does the same as All Scenes).
3. Each line lands as `<id>.mp3` in its scene's `Audio/VO` folder, Scene 4 and proposed lines included, and the scenes are rebuilt so the Timeline follows the clip lengths.

The bake remembers which words and which voice settings made each clip. After you change the voice, the model or any delivery setting, run it again: only clips made with the old voice or old words are redone. If the rate limit is reached it stops cleanly, keeps what it baked and tells you when to run it again; the next run continues where it stopped. A voice actor's `<id>.wav` or `<id>.ogg` is never replaced. Without the proxy, lines get `scratch_<id>.wav` clips from the clearest local Windows voice, so timing works before the keys exist.

## Grounded chat

Besides the question, scene and history, `/chat` accepts two optional plain-text fields from Unity: `state` (live experiment state: activity, voltage, current, recorded rows; up to 1200 characters) and `learner` (the session's learner summary; up to 500). Control characters are removed and both are appended to the system prompt as authoritative facts, so Wattson only quotes readings the app actually shows. The prompt also tells him he cannot operate anything (the app performs and confirms actions itself), to keep answers short with a deeper version on request, and to be a companion rather than a cheerleader.

## Deploy

Any Node host works. On Render, as one example:

1. New Web Service from this repository, root directory `Server/wattson-proxy`.
2. Build command `npm install`, start command `npm start`.
3. Add the keys above as environment variables.
4. Put the service's `https://...` address into `ProxyConfig.asset` as `baseUrl`. Builds always use `baseUrl`; plain `http` is only allowed in the Editor and development builds.

Railway and Fly.io work the same way: point them at this folder, `npm start`, set the keys.

## Safety

- **Rate limit:** 20 questions per 10 minutes per anonymous session id, with a looser cap per network address so a new session id does not reset it. `/tts` (`TTS_LIMIT`, default 200) and `/stt` (40) have their own limits. A blocked call gets `429` with a `Retry-After` header.
- **Input limits:** questions are capped at 300 characters, speech text at 600, live state at 1200, learner summary at 500, audio at 2 MB.
- **Personal details:** email addresses and phone numbers are removed from questions and history before anything is forwarded or logged.
- **Keyword filter:** requests to hurt someone, to make a weapon, and explicit or drug content get a fixed kid-safe reply without calling the model; self-harm gets a "talk to a trusted adult" reply. Electrical-safety questions such as "can electricity kill you?" are answered, with the safety rule. The patterns are at the top of `index.js`.
- **No storage:** the proxy keeps no conversation history. Unity sends the last six turns with each question and holds them in memory only.
- **System prompt:** `systemPrompt.txt`, editable without touching code. The scene, live-state and learner placeholders are filled from each request; text inside them is never treated as instructions.

The session id is a random GUID, not a login. Anyone who learns the proxy address can call it within the rate limits, so keep the address out of public pages and watch provider usage. If that becomes a problem, put the proxy behind your school or platform sign-in.

## Swapping providers

`synthesize(text, { previous, next })` and `transcribe(wav)` in `index.js` are the only functions that know about ElevenLabs and OpenAI. Replace the body of either one (for example with Azure Neural TTS `en-IN` voices or OpenAI TTS) and keep the return type: mp3 bytes, and plain text. Then re-bake (above) so scripted lines and live answers match again.

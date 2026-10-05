// Wattson proxy: the only place API keys live. Unity talks to /chat, /tts and /stt; nothing else.
import express from 'express';
import Anthropic from '@anthropic-ai/sdk';
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const MODEL = process.env.WATTSON_MODEL || 'claude-haiku-4-5-20251001';
const MAX_QUESTION = 300;
const MAX_SPEECH = 600;
const MAX_CONTEXT_SPEECH = 300; // previous_text / next_text for /tts
const MAX_STATE = 1200;
const MAX_LEARNER = 500;
const WINDOW_MS = 10 * 60 * 1000;
// Live answers use a few /tts calls each; a full VO bake is about 200 lines. Raise TTS_LIMIT in .env for a first full bake.
const TTS_LIMIT = Number(process.env.TTS_LIMIT) || 200;
// "Lawrence - Bright and Informative": ElevenLabs' permanent replacement for the expiring default voice "Liam" (README).
export const DEFAULT_VOICE = 'ktkP7Nsj67dw2zcplQYt';
const LOG_TEXT = process.env.LOG_TEXT === '1'; // off by default; audio is never logged
const promptTemplate = readFileSync(new URL('./systemPrompt.txt', import.meta.url), 'utf8');

// ---- safety helpers (pure, covered by check.test.js) ----

export const clean = (value, max) =>
  typeof value === 'string' ? value.replace(/[\u0000-\u001f\u007f]/g, ' ').trim().slice(0, max) : '';

// Multi-line plain text from the app (live state, learner summary): control characters out, line breaks kept, bounded.
// Not PII-filtered: it is app-generated, and the phone-number pattern would eat a row of readings.
export const cleanBlock = (value, max) =>
  typeof value === 'string'
    ? value.replace(/\r\n?/g, '\n').replace(/[\u0000-\u0009\u000b-\u001f\u007f]/g, ' ').replace(/\n{3,}/g, '\n\n').trim().slice(0, max)
    : '';

// Students sometimes type contact details; they never reach the model or a log.
export const stripPII = (text) =>
  text
    .replace(/[\w.+-]+@[\w-]+(\.[\w-]+)+/g, '[removed]')
    .replace(/(?:\+?\d[\s().-]?){8,}/g, '[removed]');

const CARE = /\b(suicide|kill(ing)? myself|self[- ]?harm|hurt(ing)? myself|want to die|end my life)\b/i;
// Harm needs an intent AND a victim, so safety questions ("can electricity kill you?", "can a shock kill a person?",
// "can a battery explode like a bomb?") reach the model, whose prompt answers them with the safety rule.
const INTENT = "(i|we)( really)? (want|wanna|plan|need|will)( to)?|(i'm|im|i am|we're|we are) (going to|gonna)" +
  '|how (do|can|could|would|should|to)( i| we| you)?|help me|teach me( how)?( to)?|ways to';
const HARM_VERB = '(kill|murder|hurt|harm|injure|electrocute|shock|poison|stab|shoot)';
const VICTIM = '(some(one|body)|people|a person|him|her|them' +
  '|my (brother|sister|friends?|teacher|mom|mum|mother|dad|father|parents?|classmates?|family|pet|dog|cat)' +
  '|an? (animal|dog|cat|kid|child|baby|person|teacher|classmate))';
const HARM = new RegExp(`\\b(${INTENT}) ${HARM_VERB} ${VICTIM}\\b`, 'i');
const WEAPON = /\b(make|build|create|craft)( a| an| my own)? (bomb|gun|weapon|explosive|taser|stun ?gun)s?\b/i;
const BLOCKED = /\b(sex|porn|nude|naked|drugs?|cocaine|weed|fuck|shit|bitch)\b/i;

// Basic keyword filter. Returns a kid-safe reply to speak instead of asking the model, or null.
export function moderate(text) {
  if (CARE.test(text)) return 'That sounds really important. Please talk to a trusted adult, like a parent or a teacher, right away.';
  if (HARM.test(text) || WEAPON.test(text) || BLOCKED.test(text)) return "That's not a case for this detective. Let's get back to our electricity mystery!";
  return null;
}

// Sliding-window limiter. Returns true when the call is allowed.
export function allow(hits, key, limit, now = Date.now()) {
  const recent = (hits.get(key) || []).filter((t) => now - t < WINDOW_MS);
  if (recent.length >= limit) { hits.set(key, recent); return false; }
  recent.push(now);
  hits.set(key, recent);
  return true;
}

// Seconds until the oldest call in the window expires, i.e. when a blocked key may call again.
export function retryAfter(hits, key, now = Date.now()) {
  const times = hits.get(key) || [];
  return times.length ? Math.max(1, Math.ceil((WINDOW_MS - (now - times[0])) / 1000)) : 1;
}

export function systemPrompt(body) {
  const list = (items) => (Array.isArray(items) ? items.slice(0, 12).map((x) => clean(x, 200)).filter(Boolean).join('; ') : '') || 'none';
  const fields = {
    sceneId: clean(body.sceneId, 40) || 'unknown',
    sceneTitle: clean(body.sceneTitle, 80),
    currentLine: clean(body.currentLine, 300),
    covered: list(body.covered),
    doNotSpoil: list(body.doNotSpoil),
    state: cleanBlock(body.state, MAX_STATE) || 'not provided',
    learner: cleanBlock(body.learner, MAX_LEARNER) || 'not provided',
  };
  // One pass over the template: text inside a field is never expanded again.
  return promptTemplate.replace(/\{(\w+)\}/g, (match, name) => fields[name] ?? match);
}

// ---- providers, each behind one function so it is easy to swap ----

// ElevenLabs voice and delivery, read from the environment on every call (README: "Voice").
export function voiceConfig(env = process.env) {
  const num = (name, fallback, min, max) => {
    const value = Number.parseFloat(env[name]);
    return Number.isFinite(value) ? Math.min(max, Math.max(min, value)) : fallback;
  };
  return {
    voice: env.ELEVENLABS_VOICE_ID || DEFAULT_VOICE,
    model: env.ELEVENLABS_MODEL || 'eleven_multilingual_v2',
    settings: {
      stability: num('ELEVENLABS_STABILITY', 0.4, 0, 1), // lower = livelier, more varied delivery
      similarity_boost: num('ELEVENLABS_SIMILARITY', 0.8, 0, 1),
      style: num('ELEVENLABS_STYLE', 0.35, 0, 1), // expressiveness
      use_speaker_boost: !/^(0|false|no)$/i.test(env.ELEVENLABS_SPEAKER_BOOST || ''),
      speed: num('ELEVENLABS_SPEED', 1.05, 0.7, 1.2), // a touch brisk and youthful; ElevenLabs allows 0.7 to 1.2
    },
  };
}

// Changes whenever the voice, model or delivery settings change. The Unity bake stores it with each clip and
// re-bakes clips made with a different one.
export const voiceSignature = ({ voice, model, settings }) => [voice, model, ...Object.values(settings).map(String)].join('|');

// ElevenLabs. Swap this body for another provider; return mp3 bytes. previous / next are the neighbouring lines, so a
// sentence split over two clips ("And when electric charge flows..." / "...we call that electric current.") keeps one melody.
export async function synthesize(text, { previous = '', next = '' } = {}) {
  const { voice, model, settings } = voiceConfig();
  const call = (body) => fetch(`https://api.elevenlabs.io/v1/text-to-speech/${encodeURIComponent(voice)}?output_format=mp3_44100_128`, {
    method: 'POST',
    headers: { 'xi-api-key': process.env.ELEVENLABS_API_KEY, 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal: AbortSignal.timeout(30000),
  });
  const full = { text, model_id: model, voice_settings: settings };
  if (previous) full.previous_text = previous;
  if (next) full.next_text = next;
  let response = await call(full);
  if (response.status === 400 || response.status === 422) {
    // A model that rejects part of this (eleven_v3 takes no previous/next text and only 0, 0.5 or 1 stability) still
    // speaks with its own defaults instead of failing the line.
    console.warn(`tts: ${model} rejected the voice settings (${response.status}); retrying with its defaults`);
    response = await call({ text, model_id: model });
  }
  if (!response.ok) throw new Error('tts provider ' + response.status);
  return Buffer.from(await response.arrayBuffer());
}

// OpenAI transcription. Swap this body for another speech-to-text service; return the text.
async function transcribe(wav) {
  const form = new FormData();
  form.append('file', new Blob([wav], { type: 'audio/wav' }), 'question.wav');
  form.append('model', process.env.STT_MODEL || 'gpt-4o-mini-transcribe');
  const response = await fetch('https://api.openai.com/v1/audio/transcriptions', {
    method: 'POST',
    headers: { Authorization: 'Bearer ' + process.env.OPENAI_API_KEY },
    body: form,
    signal: AbortSignal.timeout(20000),
  });
  if (!response.ok) throw new Error('stt provider ' + response.status);
  return (await response.json()).text || '';
}

// ---- server ----

export const app = express();
app.set('trust proxy', 1);

const hits = new Map();
setInterval(() => {
  const now = Date.now();
  for (const [key, times] of hits) if (times.every((t) => now - t >= WINDOW_MS)) hits.delete(key);
}, WINDOW_MS).unref();

// Per anonymous session, plus a looser per-address cap so rotating session ids does not get around it.
// Retry-After tells the caller (the VO bake) when it may continue.
const limited = (name, perSession) => (req, res, next) => {
  const id = req.get('X-Session-Id');
  const session = /^[a-f0-9]{32}$/.test(id || '') ? id : req.ip;
  for (const [key, limit] of [[`${name}:s:${session}`, perSession], [`${name}:ip:${req.ip}`, perSession * 15]]) {
    if (allow(hits, key, limit)) continue;
    res.set('Retry-After', String(retryAfter(hits, key)));
    return res.status(429).json({ error: 'rate_limited' });
  }
  next();
};

const needs = (envName) => (req, res, next) =>
  process.env[envName] ? next() : res.status(503).json({ error: 'not_configured', missing: envName });

// ttsVoice is configuration, not a secret: voice id, model and delivery settings (the bake's re-bake signature).
app.get('/health', (req, res) => res.json({
  ok: true, model: MODEL,
  chat: !!process.env.ANTHROPIC_API_KEY, tts: !!process.env.ELEVENLABS_API_KEY, stt: !!process.env.OPENAI_API_KEY,
  ttsVoice: voiceSignature(voiceConfig()),
}));

let anthropic;
app.post('/chat', express.json({ limit: '16kb' }), needs('ANTHROPIC_API_KEY'), limited('chat', 20), async (req, res) => {
  const body = req.body || {};
  if (typeof body.question !== 'string' || body.question.length > MAX_QUESTION) return res.status(400).json({ error: 'invalid_question' });
  const question = stripPII(clean(body.question, MAX_QUESTION));
  if (!question) return res.status(400).json({ error: 'invalid_question' });
  if (LOG_TEXT) console.log('Q:', question);

  res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-cache', 'X-Accel-Buffering': 'no' });
  const send = (data) => res.write(`data: ${JSON.stringify(data)}\n\n`);
  const finish = () => { send({ done: true }); res.end(); };

  const canned = moderate(question);
  if (canned) { send({ t: canned }); return finish(); }

  // Last six turns only; the proxy keeps no conversation state of its own.
  const history = (Array.isArray(body.history) ? body.history : []).slice(-12)
    .filter((turn) => turn && (turn.role === 'user' || turn.role === 'assistant') && clean(turn.text, 1) !== '')
    .map((turn) => ({ role: turn.role, content: stripPII(clean(turn.text, 800)) }));
  while (history.length && history[0].role !== 'user') history.shift();

  anthropic ??= new Anthropic();
  const stream = anthropic.messages.stream({
    model: MODEL,
    max_tokens: 300, // short answers by default, with room for the deeper version when the learner asks for it
    system: systemPrompt(body),
    messages: [...history, { role: 'user', content: question }],
  });
  res.on('close', () => stream.abort());
  let spoke = false;
  stream.on('text', (text) => { spoke = true; send({ t: text }); });
  try {
    const message = await stream.finalMessage();
    if (!spoke) send({ t: "Hmm, that one is outside this case. Ask me something about electricity, detective!" });
    if (LOG_TEXT) console.log('A:', message.content.map((block) => block.text || '').join(''));
  } catch (error) {
    if (error instanceof Anthropic.APIUserAbortError) return res.end();
    console.error('chat failed:', error instanceof Anthropic.APIError ? `${error.status} ${error.name}` : error.message);
    if (error instanceof Anthropic.RateLimitError) send({ error: 'busy' });
    else if (error instanceof Anthropic.APIConnectionError) send({ error: 'unreachable' });
    else send({ error: 'upstream' });
  }
  finish();
});

app.post('/tts', express.json({ limit: '16kb' }), needs('ELEVENLABS_API_KEY'), limited('tts', TTS_LIMIT), async (req, res) => {
  const text = clean(req.body?.text, MAX_SPEECH);
  if (!text) return res.status(400).json({ error: 'invalid_text' });
  const previous = clean(req.body?.previous_text, MAX_CONTEXT_SPEECH);
  const next = clean(req.body?.next_text, MAX_CONTEXT_SPEECH);
  try {
    res.type('audio/mpeg').send(await synthesize(text, { previous, next }));
  } catch (error) {
    console.error('tts failed:', error.message);
    res.status(502).json({ error: 'tts_failed' });
  }
});

app.post('/stt', express.raw({ type: 'audio/wav', limit: '2mb' }), needs('OPENAI_API_KEY'), limited('stt', 40), async (req, res) => {
  if (!Buffer.isBuffer(req.body) || req.body.length < 1000) return res.status(400).json({ error: 'invalid_audio' });
  try {
    res.json({ text: stripPII(clean(await transcribe(req.body), MAX_QUESTION)) });
  } catch (error) {
    console.error('stt failed:', error.message);
    res.status(502).json({ error: 'stt_failed' });
  }
});

if (import.meta.url === pathToFileURL(process.argv[1] || '').href) {
  const port = Number(process.env.PORT) || 3000;
  app.listen(port, () => console.log(`Wattson proxy on http://localhost:${port} (model ${MODEL}, voice ${voiceConfig().voice})`));
}

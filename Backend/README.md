# Wattson Google backend

This local prototype listens on `127.0.0.1:8787`. The provider key remains outside Unity, encrypted by Windows DPAPI for the current Windows account in `Local/google-key.dpapi`. Do not share the Local directory. No chat content or API credential is logged.

Run `Start-Wattson.ps1 -NodePath <node executable>` before playing the desktop lesson. Node 20 or later is required. `GET /health` reports readiness; `POST /chat` accepts the existing Unity message/context/history contract. The default model is gemini-3.8-flash; WATTSON_MODEL can override it. The key was verified against Google's model-list endpoint. Grading and scene progression remain in Unity. A model can still make mistakes; retain authored assessments and review its educational responses.

The launcher decrypts the credential into only the child process environment. On other server environments, inject GEMINI_API_KEY through that environment's secret manager. Never add it to Unity or use it as a command-line argument.

For a replacement key, use PowerShell `Read-Host -AsSecureString` interactively, pipe the result through `ConvertFrom-SecureString`, and save it to the same DPAPI file. This keeps the value out of command history. Restart the backend after replacing it.

This server intentionally accepts loopback connections only and rejects browser-origin requests. It provides body limits, timeouts and a small request limit. Android requires a separately hosted HTTPS backend with authentication and per-user quotas; localhost on a tablet is the tablet itself. Set Android Chat Endpoint only after deploying that service. No public deployment is made by this prototype.

To stop the server, stop the specific process recorded in Local/server.pid after checking it is the Wattson Node process. Startup is manual, not a scheduled task or a Windows service.

API contract: https://ai.google.dev/api

## Web-connected conversation

Requests enable Google's `google_search` tool. Wattson can answer general questions, clarify incomplete speech, and use the current lesson context without changing assessment scores. The model decides when search is useful. A response is marked web-checked only when Google returns search metadata.

`POST /chat` also returns `searched`, `sources`, and `sourcesPath`. The Unity conversation offers a Google Search sources button. It opens the answer, source links and Google's original Search Suggestions widget in a browser. These pages stay in server memory for 30 minutes and expire on restart; chat text is not written to disk. Only HTTPS source links are accepted, and the page disallows scripts and forms.

Temporary HTTP 503 capacity errors receive one bounded retry. Quota/access errors are not retried. Connected chat never substitutes a canned lesson paragraph for a failed live reply; it retains the question and reports the service problem. Explicit offline configurations still provide the local lesson guide.

Verification on 2026-09-29: backend unit tests pass, the model-list endpoint accepts the encrypted credential, but a live grounded request returns HTTP 429 (`provider_quota`). An alternate-model diagnostic returned HTTP 503 capacity unavailable. Live web answers cannot be verified until the Google project's quota/service is available. Check quota and billing in Google AI Studio; no Unity credential changes are required.


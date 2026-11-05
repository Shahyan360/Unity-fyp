# Hybrid: local answers with Gemini speech

Hybrid mode now follows this flow for typed and spoken questions:

1. Microphone input is transcribed locally by faster-whisper. Typed input enters the same conversation path directly.
2. Ollama generates the answer using the existing character, memory, world and reference context. Deterministic project actions and reference fallbacks remain local.
3. Text streams into the transcript. The completed reply is sent once to Gemini TTS, which returns audio in the selected character voice.
4. Unity plays the audio, drives the speaking character, and supports cancellation. A local Windows voice is used if Gemini synthesis is unavailable; the local answer remains visible.

The Gemini request contains only reply text and voice configuration. It contains no visitor-question/history prompt or tools. Hybrid does not open a Gemini Live session, and the backend rejects Live-answer connections unless the configured mode is `gemini`. The separate `local` and `gemini` modes retain their existing roles.

Speech defaults to `gemini-3.8-flash-tts`, configurable with `LUMEN_GEMINI_TTS_MODEL`. Character voices come from `LumenCharacterIdentities.json`; ambient characters receive stable voice selections. The API key stays in the backend. Google describes the dedicated [TTS API](https://ai.google.dev/gemini-api/docs/speech-generation) as text recitation, distinct from interactive Live conversation.

Speech requests are bounded. Two cloud requests can coexist so an interrupted request can finish while the next turn starts. Cancelling playback aborts the Unity HTTP request and clears queued audio. Runtime setup now reuses one speech controller so playback, volume and cancellation target the same component.

## Start

Stop an existing backend with Ctrl+C, then double-click `Start-Lumen-Hybrid.cmd` in the repository root. Leave its window open and run:

`Builds/HybridVoice-20261105/Windows-Verified/Lumen.exe`

Alternatively set `$env:LUMEN_CONVERSATION_PROVIDER = "hybrid"` and run `Tools/Start-LumenBackend.ps1` from the same PowerShell session. Gemini speech requires internet and the saved key. Local recognition requires the installed speech model. Normal local mode remains available without cloud speech.

## Evidence

- Backend and packaging suite: **122 passed**, including exact TTS input, local answer provenance, character voice selection, audio decoding, error sanitization, bounded concurrent requests and runtime relocation. The staged hybrid-backend import test also passes.
- Real saved-key speech probes returned non-silent, mono 24000 Hz audio. Flash TTS took 11.43 seconds for a 5.92-second sample; Flash-Lite took 11.83 seconds for a 5-second sample. These are individual samples, not a latency guarantee. Response quality/speed optimization is deferred.
- A real WebSocket probe confirmed hybrid rejects Gemini Live answer sessions. The local recognizer reports ready.
- Windows build succeeded with zero errors, GUID `530eedc122984a43a48a5395bbc94164`, 698,881,773 bytes. The 502 warnings originate in existing package shaders, optional Pipeline runtime configuration and collision import settings.
- Standalone typed and recorded-speech verification passed **15/15 checks**, with zero failures: both answers came from Ollama, both played non-silent Gemini audio, the switched guide selected its voice, Gemini Live stayed disconnected, and cancellation cleared pending speech/playback. Evidence: `Player-Verified/hybrid-voice.json` under `Builds/HybridVoice-20261105/`. Earlier diagnostic builds/reports are retained separately.

Natural human microphone/noise/echo and perceived voice quality still require human acceptance. The recorded-input check uses a synthetic WAV, not a recording of the user's microphone. No response-quality optimization, package upgrade, scene rewrite or key/configuration persistence change was performed.

Changes remain local and uncommitted. Raw audio, build logs and test profiles are in ignored `Builds/HybridVoice-20261105/`.
Original font/build/project settings were restored byte-for-byte after validation. The temporary test backend on port 5001 was stopped; the user's existing Ollama services were preserved.

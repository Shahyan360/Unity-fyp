# Lumen backend: local-first conversations

The Unity client connects to Python at `http://127.0.0.1:5000` and defaults to
`/chat/stream` with a local Ollama model. Local faster-whisper recognizes speech;
Windows voices or optional Piper speak streamed sentences.
See [research, setup and measured results](../Docs/AI/LocalFirstInference-2026-11-02.md).

## Install once

Use Python 3.12. From the repository root:

```powershell
py -3.12 -m venv embodied-ai-backend/.venv
./embodied-ai-backend/.venv/Scripts/python.exe -m pip install --upgrade pip
./embodied-ai-backend/.venv/Scripts/python.exe -m pip install -r embodied-ai-backend/requirements-voice.txt
```

Install `requirements-voice.txt`, which includes the basic requirements plus
Google GenAI, Flask WebSocket support, and local speech recognition. Installing
only `requirements.txt` does not install everything needed to start this service.
If `.venv/Scripts/python.exe` already exists, use it directly; Python does not
need reinstalling just because the Windows Python launcher cannot find it.

## Optional Gemini Live voice

Set `LUMEN_CONVERSATION_PROVIDER=hybrid` for native Gemini microphone
conversations and distinct guide voices. Typed answers still go to local Ollama.
Gemini handles the spoken exchange while Unity executes world actions and
displays images. If Live cannot connect, voice input uses local faster-whisper.
The default `local` mode keeps speech and text local. The existing `gemini`
mode also routes typed conversation through Live.

Create a key in [Google AI Studio](https://aistudio.google.com/apikey).
Use PowerShell to save it without displaying it or recording its value in history:

```powershell
$secureKey = Read-Host 'Paste your Gemini API key' -AsSecureString
$credential = [System.Net.NetworkCredential]::new('', $secureKey)
$env:GEMINI_API_KEY = $credential.Password
[Environment]::SetEnvironmentVariable('GEMINI_API_KEY', $credential.Password, 'User')
Remove-Variable credential, secureKey
```

An existing Windows user key is read automatically by the backend.
Never paste it into chat, Unity scripts, settings committed to Git, or logs.

## Start every time you use Unity

Double-click `Start-Lumen-Backend.cmd` in the repository root, or run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./Tools/Start-LumenBackend.ps1
```

Keep that window open. Stop it with Ctrl+C. The launcher preserves an existing
service on port 5000 instead of killing it. `-CheckOnly` reports setup without
starting the service or revealing the key.

Check [service health](http://127.0.0.1:5000/health) for `"status": "ok"`.
Check [conversation configuration](http://127.0.0.1:5000/conversation/config) for
`"provider": "local"`, and `/health` for `configured_model_available=true`.
Send a short typed question in Unity to verify the integration.

Open the Unity folder containing this backend, not an older copy.
Open `Assets/Scenes/HomeScene.unity` in Unity 6000.6.2f1, press Play, and keep
offline mode disabled. Test typed conversation before enabling the microphone.

## Models and optional offline features

The live model defaults to `gemini-3.8-live`. If your account cannot access it,
verify the models available to your key and set `GEMINI_LIVE_MODEL` in the
backend environment. See [Google's Live API guide](https://ai.google.dev/gemini-api/docs/live-api/get-started-sdk).

The `/chat/stream` route generates ordinary answers with the local model.
Deterministic identities, world/exhibit actions and preference recall remain local
application logic. `/chat` retains the older JSON contract for compatibility.
Override the selected model with `OLLAMA_MODEL`; install it using `ollama pull`.
The launcher warms the selected model on a separate CUDA 12 Ollama server at
127.0.0.1:11435 on this machine. Original Ollama on 11434 is preserved.

Gemini microphone audio is sent through the live connection. The separate
local `/transcribe` feature needs a faster-whisper model in
`embodied-ai-backend/runtime/models/base.en`, or a directory selected by
`LUMEN_STT_MODEL`. These model files are not included in this checkout.
Optional Piper files belong in `Runtime/Piper`, or `LUMEN_PIPER_HOME`;
Unity falls back to installed Windows voices when Piper is absent.

## Tests

From the repository root:

```powershell
./embodied-ai-backend/.venv/Scripts/python.exe -m pytest -q -p no:cacheprovider
```

Tests use local fixtures. Passing tests alone do not verify a real Gemini key,
microphone permission, Unity compilation, or visual behavior.


## Consistent local neural speech

Run `powershell -NoProfile -ExecutionPolicy Bypass -File ./Tools/Install-LumenNeuralVoice.ps1 -IncludeRecognition` from the repository root once, then restart the backend. It keeps Piper loaded and exposes `/speech/config` and `/speech/synthesize`. Typed and microphone questions use the same character voice. Models and runtime files stay outside Git. The model card is downloaded alongside the voice.

The microphone starts off. Press M or click its control for a 12-second listening window, or hold V to speak. The visible countdown bounds the capture. This requires deliberate activation instead of attempting to infer who background speech addresses. The optional `-IncludeRecognition` switch also installs the local English recognition model.

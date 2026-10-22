# Lumen backend: Gemini live conversations

The current Unity client connects to Python at `http://127.0.0.1:5000` and uses
`/live` for Gemini text and microphone conversations. The key belongs only in
the Python backend environment. Unity does not need an API key field.

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

## Configure the key privately

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
Check [Gemini configuration](http://127.0.0.1:5000/live/status) for
`"configured": true`. Neither response proves that a Gemini conversation works;
send a short typed question in Unity to verify it.

Open the Unity folder containing this backend, not an older copy.
Open `Assets/Scenes/HomeScene.unity` in Unity 6000.6.2f1, press Play, and keep
offline mode disabled. Test typed conversation before enabling the microphone.

## Models and optional offline features

The live model defaults to `gemini-3.8-live`. If your account cannot access it,
verify the models available to your key and set `GEMINI_LIVE_MODEL` in the
backend environment. See [Google's Live API guide](https://ai.google.dev/gemini-api/docs/live-api/get-started-sdk).

The legacy `/chat` and `/chat/stream` routes use curated local replies and
optional Ollama (`llama3.2:1b`). An unavailable Ollama model in `/health` does
not mean Gemini live is broken. Gemini uses `/live`, not the Ollama chat route.

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

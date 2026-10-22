## Local setup verified on 2026-10-22

This section supersedes historical machine paths and backend setup claims below.

- Project root: `C:\Users\dell 5410\Unityproject\Unity-fyp-merge`.
- Branch: `codex/lumen-finalization-20261020`; commit `f0c5701`. Fetch and explicit
  fast-forward pull succeeded. Working tree was clean before this investigation.
- `C:\Users\dell 5410\Unityproject\EmboidedAI` is a separate older copy whose
  backend lacks `gemini_live.py`; use Unity-fyp-merge for Gemini.
- Unity 6000.6.2f1 is installed. Packages include URP 17.6.0, Input System 1.20.0,
  AI Navigation 2.0.16, UGUI 2.6.0 and Pipeline 0.8.0-exp.1. No connected Editor
  was found. A hidden batch launch exited without a validation log. Access to
  Unity's per-user settings directories was not granted, so compilation, Play
  mode, rendering and microphone capture remain unverified in this session.
- Startup scene: HomeScene. Enabled build scenes: HomeScene, Classroom,
  Cafestore, HistoricalPlace. Phase1_DemoScene is present but disabled.
- Unity uses the Python `/live` WebSocket for Gemini conversations. The saved
  Windows user key is read by `gemini_live.api_key()` without putting it in
  Unity. Google model discovery accepted the existing key and included
  `gemini-3.8-live`; actual conversation verification is recorded separately.
- The legacy `/chat` routes still use optional Ollama and curated local replies.
  Ollama availability in `/health` is independent of Gemini live availability.
- Python 3.12.15 is installed locally inside the ignored backend `.venv`, with
  its base interpreter under `.venv/python-runtime/python`. This setup does not
  rely on the Windows `py` launcher (which reported no registered versions).
- Start the service with root `Start-Lumen-Backend.cmd` or
  `Tools/Start-LumenBackend.ps1`; the latter supports `-CheckOnly`.
- Local Whisper model and optional Piper runtime/voice files are absent. Gemini
  live audio does not require these offline feature files.
- Backend tests are configured by root `pytest.ini`; use `.venv/Scripts/python.exe`
  with `-m pytest -q -p no:cacheprovider` from the repository root.
- Do not expose the API key, remove local caches, change scenes blindly, or treat
  historical test/build claims below as fresh verification.


## Validation results

- Installed `requirements-voice.txt`; `pip check`: no broken requirements.
- Backend suite: **86 passed in 12.28 seconds**.
- Direct Gemini live conversation: connected, received transcript and 109442
  audio bytes using the existing saved key and default model.
- Actual `gemini_live.bridge` logic with a simulated Unity transport and a real
  Gemini session: emitted ready/output_text/turn_complete and 253440 audio bytes.
  This proves provider/bridge behavior, not the actual Unity WebSocket transport.
- Flask test-client checks: `/health` and `/live/status` returned HTTP 200;
  health status was ok and Gemini configured was true. `/voice/health` confirms
  the optional local recognition model is absent.
- The launcher started Flask on 127.0.0.1:5000; external loopback probes from
  the tool environment timed out. The owned test server was stopped with Ctrl+C.
  Start it through the desktop launcher for the Unity session.
- Scene references resolve to Assets/package cache except the old BackgroundFloor
  material in HomeScene. `WalkableHomeHub.Ensure()` disables that retired plane;
  no scene edits were made for it. This was a static scan, not Editor validation.
- PowerShell launcher syntax parsed without errors and `-CheckOnly` reported
  Python available and saved key present, without exposing credential values.
- No Unity runtime scripts, scenes, packages or project settings were changed.
- Unity compilation, actual HomeScene Play mode, microphone capture and a
  Windows player build remain unverified. Unity's settings-folder access was
  not granted; the hidden validation process exited without producing a log.

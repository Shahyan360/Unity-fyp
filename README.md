# Embodied AI Unity Experience

Conversation now defaults to a local Ollama LLM, with streamed text and local
speech. Start `Tools/Start-LumenBackend.ps1` before pressing Play. A saved Gemini
key no longer selects cloud conversation automatically. See
[local-first setup, research and measurements](Docs/AI/LocalFirstInference-2026-11-02.md).
Set `LUMEN_CONVERSATION_PROVIDER=hybrid` to keep answers and microphone recognition
local while Gemini TTS supplies character voices. See [hybrid voice setup](embodied-ai-backend/README.md#optional-gemini-speech).

Current development and verified results are tracked in [PROJECT_PROGRESS.md](PROJECT_PROGRESS.md). The older completion guide contains historical results. The current upgrade includes Ada (science), Marcus (history), and Maya (café), optional local Piper neural speech, and a Blender-authored prop collection; broader environment and release work remains in progress.

Unity `6000.6.2f1` project with three locally guided worlds: Ancient Rome, a science classroom, and a café conversation/order-practice scene.

## Run

1. Open this folder in Unity `6000.6.2f1` and wait for the initial import and script compile to finish.
2. Start the services using [`embodied-ai-backend/README.md`](embodied-ai-backend/README.md). Local mode needs no cloud key; hybrid uses your saved Gemini key for speech.
3. Open `Assets/Scenes/HomeScene.unity` if the Lumen hub is not already open, then press Play.
4. Choose a world card. Use WASD to move. The cursor starts free so you can use the guide; right-click on the 3D world to enter mouse-look, then press `Esc` to release it. Left-click remains available for buttons. Press `E` while looking at an object/person to ask about it. The Worlds button returns to the hub.

The café menu is a simulation and does not charge money. See [`PROJECT_COMPLETION.md`](PROJECT_COMPLETION.md) for feature coverage, controls, test results, and remaining production work.

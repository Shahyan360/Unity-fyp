# Lumen

Lumen is a Unity experience with a walkable hub and four guided destinations. It uses Unity 6000.6.2f1 and a local Python backend.

## Run locally

1. Open this folder in Unity 6000.6.2f1. The startup scene is `Assets/Scenes/HomeScene.unity`.
2. Start `Start-Lumen-Backend.cmd` for local conversation and speech, or `Start-Lumen-Hybrid.cmd` for local conversation with Gemini speech. See the [backend setup guide](embodied-ai-backend/README.md) for Python, Ollama, and voice configuration.
3. Press Play in Unity and choose a destination. Use WASD to move, right-click to look around, Esc to release the cursor, and E to interact. Use **Worlds** to return to the hub.

## Project layout

| Folder | What belongs there |
| --- | --- |
| `Assets/Scenes` | The hub, science classroom, café, and Ancient Rome. The museum uses the classroom scene with its own runtime setup. |
| `Assets/Scripts/Worlds` | Hub, café, classroom, museum, and Rome behavior. `Shared` contains setup, scene transitions, and navigation used across worlds. |
| `Assets/Scripts/Agent` | Guide appearance, conversation, voice, and navigation. |
| `Assets/Scripts/Home`, `UI`, `Player`, `Interaction` | Home chat, menus and in-world UI, player controls, and object interactions. |
| `Assets/Scripts/Backend`, `Settings`, `Audio` | Unity API client and message models, preferences, and audio mixing. |
| `Assets/Scripts/Validation` | Opt-in player verification routines. Recorded test inputs are in `Assets/StreamingAssets/Verification`. |
| `Assets/Editor` | Editor-only art, build, world setup, and validation tools. |
| `Assets/Art`, `Materials`, `Navigation`, `Prefabs` | Character and prop source art, scene materials, baked navigation, and authoring prefabs. |
| `Assets/Resources` | Named assets loaded at runtime, including characters, destination previews, exhibits, and audio. |
| `Assets/ThirdParty`, `Assets/TextMesh Pro` | Imported models, textures, animations, licenses, and text rendering assets. |
| `Assets/Settings`, `Packages`, `ProjectSettings` | Input and rendering assets, Unity package versions, and project configuration. |
| `embodied-ai-backend` | Flask service, Ollama and speech integrations, knowledge data, and backend tests. |
| `Tools`, `Docs` | Launch, release, test and asset tooling, plus project documentation. |

Unity regenerates `Library`, `Logs`, and IDE files as needed. The backend's `.venv` is local to this checkout. Keep an asset and its `.meta` together when moving either from the filesystem.

## Build and test

Use `LumenReleaseBuild.Build` for a Windows x64 player. See the [release runbook](Docs/AI/ReleaseRunbook.md) for build and packaging commands.

Run the backend suite from this folder after installing the voice requirements.

```powershell
.\embodied-ai-backend\.venv\Scripts\python.exe -m pytest -q -p no:cacheprovider
```

See [Unity project context](Docs/AI/UnityProjectContext.md) for the scene routes and runtime entry points. [Project progress](PROJECT_PROGRESS.md) and the [completion guide](PROJECT_COMPLETION.md) record earlier milestones.

For the browser target, see the [web release handoff](Docs/AI/WebRelease.md).

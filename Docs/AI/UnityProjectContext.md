# Unity Project Context
> Current release continuation and missing-evidence inventory: [ReleaseContinuation-2026-10-22.md](ReleaseContinuation-2026-10-22.md). Earlier machine paths and validation claims below are historical.
> Current machine and Gemini setup: see [LocalSetup-2026-10-22.md](LocalSetup-2026-10-22.md). Historical backend, path and validation claims below may be outdated.


<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:\Users\HP\Documents\Codex\EmbodiedAI_Unity_Client - AssestsTesting`
- Product: Embodied AI / Lumen, a locally guided Unity experience with a walkable hub, Ancient Rome, classroom, café, and a museum variant hosted in the classroom scene; a Flask backend optionally uses local Ollama.
- Last analyzed: 2026-10-15 (Asia/Karachi)
- Resume verification: 2026-10-17. Current expanded frontend build and standalone52/52 acceptance journey are documented in `Docs/AI/FrontendValidation-2026-10-17.md`. Read `PROJECT_PROGRESS.md` first for current evidence, limitations, tool paths and exact next steps. The sections below retain the original onboarding baseline; old Kyle, scene-duplicate, speech, UI and test-count claims there predate the upgrade.
- Last analyzed commit: `1a383da` (`Update TMP fallback font asset`), branch `fix/npc-player-grounding`, tracking `origin/fix/npc-player-grounding`.
- Working tree at analysis: dirty. Preserve current edits in `Assets/Scenes/HomeScene.unity`, `Assets/Scenes/HomeScene 1.unity`, `Assets/Scripts/Runtime/HubCardHover.cs`, `Assets/Scripts/Runtime/WalkableHomeHub.cs`, `Assets/Scripts/Runtime/WorldExperienceDirector.cs`; untracked `Assets/Scripts/Runtime/HomeScenarioPanel.cs` (+ `.meta`) and `Assets/Resources/LumenGalaxySky.mat` (+ `.meta`). Do not reset/checkout these changes.

## Confirmed Environment

- Unity version: `6000.6.2f1` (revision `770e33f6875c`).
- Render pipeline: URP (`com.unity.render-pipelines.universal` 17.6.0 and URP global settings).
- Input: Input System package 1.20.0 with project input-action config; some player scripts may use specific input APIs, so inspect before changing.
- Target platforms: not established from the inspected settings. Windows is the current development environment, not proof of configured release targets.
- Local backend: Python Flask service in `embodied-ai-backend`; Ollama is optional for deterministic/curated answers and required for local model generation. Default configured model `llama3.2:1b`.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.6.0 | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Input | Input System 1.20.0 | Confirmed | `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset` input settings |
| Navigation | AI Navigation 2.0.16; NavMesh agents and runtime-built surfaces used | Confirmed | package manifest; `Assets/Scripts/Runtime/MuseumGalleryExperience.cs`, agent/crowd scripts |
| UI | UGUI 2.6.0, TextMesh Pro assets | Confirmed | package manifest and source |
| Unity AI packages | AI Assistant 2.20.0-pre.2 and AI Inference 2.6.1 are installed; not evidence that project conversations depend on cloud AI | Confirmed | package manifest; backend/client source |
| Networking | No multiplayer/networking package or first-party networking system identified | Confirmed within inspected dependencies/source | manifest and first-party scripts |
| Addressables/DOTS | Not present in manifest | Confirmed | `Packages/manifest.json` |
| Assemblies | No first-party runtime/test asmdefs found. One `Assets/UnityTechnologies/Editor/URPWizard.asmdef` belongs to imported/editor tooling. | Confirmed | `Assets/**/*.asmdef` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scenes/` | Home, classroom, café, historical Rome, phase/demo, and duplicate `HomeScene 1` scenes | Confirmed | scene files and build settings |
| `Assets/Scripts/Agent/`, `API/`, `Config/`, `Home/`, `Interaction/`, `Player/`, `Runtime/`, `UI/` | First-party companion, API, scenario/world runtime, locomotion, object, voice, and UI code | Confirmed | source inventory |
| `Assets/Resources/LumenGuide/` | Runtime-loadable Robot Kyle prefab | Confirmed | `RobotKyle.prefab` |
| `Assets/Environments/`, `ImportedAssets/`, `UnityTechnologies/`, named art folders | Authored/imported environments, props, and character assets; review licenses before redistribution | Confirmed for folders; asset provenance varies | `Assets/` inventory |
| `Assets/Resources/` | Runtime-loadable resources including current untracked Lumen skybox material | Confirmed | source tree and current Git status |
| `embodied-ai-backend/app.py` | Flask API, local Ollama adapter, scenario routing, curated immediate knowledge/fallback | Confirmed | backend source |
| `embodied-ai-backend/data/` | Scenario knowledge text files | Confirmed | backend source references |
| `embodied-ai-backend/tests/test_app.py` | Backend API and intent-routing tests (26 named test functions) | Confirmed | test source |
| `Library/`, `Temp/`, `Logs/`, `obj/`, `embodied-ai-backend/.venv/` | Generated/local caches and environment; do not treat as source or remove without a user-approved cleanup scope | Likely/standard Unity and Python layout | directory inventory |

## Scenes And Startup Flow

- Enabled Build Settings scenes, in order: `HomeScene`, `Classroom`, `Cafestore`, `HistoricalPlace`, `Phase1_DemoScene` (`ProjectSettings/EditorBuildSettings.asset`).
- Startup scene is `Assets/Scenes/HomeScene.unity` (first enabled scene). A second `HomeScene 1.unity` exists and is explicitly mentioned by the runtime hub code/docs; determine whether it is a needed duplicate before changing/removing it.
- `ScenarioTransitionService` validates scenario aliases, updates `PlayerPrefs`, performs a short fade, then loads: `ancient_rome` → `HistoricalPlace`; `classroom` → `Classroom`; `cafe` → `Cafestore`; `museum` → `Classroom` with a museum runtime variant. “Worlds” routes home.
- `WorldExperienceDirector` / `EmbodiedExperienceBootstrap` add shared systems at runtime. `WalkableHomeHub` constructs a walkable hub, portals, player/camera, and guide at runtime. Current working-tree edits substantially change the hub toward a dark nebula/rift visual treatment and add a reveal/hide world-card panel; this is uncommitted and should be inspected in Unity before assuming visual success.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Unity gameplay | MonoBehaviour and static runtime-builder centric; world systems often create UI, exhibits, and environmental geometry dynamically | Confirmed | `Assets/Scripts/Runtime/WalkableHomeHub.cs`, `WorldExperienceDirector.cs`, `ClassroomLearningExhibit.cs`, `MuseumGalleryExperience.cs` |
| Cross-world setup | Runtime bootstrap/director ensures guide, UI, audio, NPC and player components; scene-load callbacks | Confirmed | `EmbodiedExperienceBootstrap.cs`, `WorldExperienceDirector.cs` |
| Scene transitions | Central scenario-to-scene route table, PlayerPrefs selection, async single-scene load, overlay fade | Confirmed | `ScenarioTransitionService.cs`, `ScenarioRuntimeManager.cs` |
| Unity-to-AI path | `AIClient` posts JSON to `http://127.0.0.1:5000/chat`; response handler drives text/action/voice/scene or exhibit intents | Confirmed | `Assets/Scripts/API/AIClient.cs`, `AgentResponseHandler.cs`, backend `app.py` |
| Local AI | Flask stores in-memory sessions, queries Ollama at `127.0.0.1:11434`, validates structured output, and has quick deterministic knowledge/fallback routes | Confirmed | `embodied-ai-backend/app.py`, backend README |
| Persistence | Current scenario selection in `PlayerPrefs`; chat session held in process/in-memory backend. No durable save architecture identified. | Confirmed for these systems | `AppConfig.cs`, backend `Session` |
| Networking | Single-machine localhost design; no multiplayer functionality identified | Confirmed within inspected code | manifest and backend/client URLs |

## Key Experiences

- Hub: walkable garden/hub with Rome, classroom, café, and museum portals; latest working-tree design is being revised (space/nebula ambience, animated portal rifts, interactive/revealable scenario panel). This version has not been visually validated after the current edits.
- Ancient Rome (`HistoricalPlace`): authored historic scene plus runtime guide/NPC setup; intended guided exploration of Forum/Colosseum/temple/fountain. Interpretive or reconstructed content should be labeled appropriately.
- Classroom (`Classroom`): starts sparse; exhibit generated on request. Catalog code includes atom, solar system and individual planets, periodic table, plant/animal cells, DNA, heart, water cycle, water molecule, circuit, volcano, Pythagorean theorem, plants, animal anatomy, sunflower, oak, light spectrum, Triceratops, fern, Apollo 11, etc. Most procedural visuals are stylized teaching models, not photorealistic sourced assets.
- Museum: runtime gallery variant routed through the `Classroom` scene. `MuseumGalleryExperience` builds artifact replicas/labels/interactions and has 12 catalog entries, including Rosetta Stone, Antikythera Mechanism, Roman Coin, Terracotta Warrior, Greek Amphora, Venus de Milo, Sutton Hoo Helmet, Gutenberg Press, Triceratops, Fern, Apollo 11 Command Module, and George Washington bust. These are primitive/procedural representations except named Smithsonian digital model paths where available; confirm actual imported model availability before claiming all are detailed 3D scans.
- Café (`Cafestore`): first-person controller, NPC/guest routines, conversation, simulated menu/order/pick-up/table service. Orders do not charge real money and there is no POS/payment integration.
- Guide: existing Robot Kyle prefab, companion NavMesh following, animator/procedural gesture behavior, object explanation, voice output, and Windows push-to-talk code. Custom facial animation/lip sync and authored high-end locomotion animation are not established.

## Coding Conventions

- Mostly global namespace; PascalCase types/methods, camelCase serialized/public fields, private serialized fields typically camelCase; braces on new lines. Confirm against adjacent source before editing.
- Runtime scripts generally use Unity callbacks and coroutines/UnityWebRequest; backend uses Python type hints, Flask routes, dataclasses, and pytest.
- Keep edits localized, preserve all current user changes, and do not replace the installed Unity editor version. Avoid adding Asset Store dependencies without checking render-pipeline compatibility, licenses, repo size, and import impact.

## Testing And Validation

- Backend pytest suite exists: 26 test functions in `embodied-ai-backend/tests/test_app.py`. Documented invocation: `cd embodied-ai-backend; .\.venv\Scripts\python.exe -m pytest -q -p no:cacheprovider` (or install `requirements.txt` in a venv). Tests were not executed during this inspection; earlier documentation reports prior passes, but that is historical, not a fresh result.
- Unity Test Framework appears transitively in the package lock, but no project EditMode/PlayMode test source directories or test asmdefs were found. Do not claim Unity automated coverage.
- Build settings include five scenes, but no standalone build/CI workflow was identified. Current uncommitted scene/runtime changes require Unity script reload, Console check, Play-mode click-through, and build validation.
- Historical `PROJECT_COMPLETION.md` says the editor was open and grounding checks compiled/tested earlier, but explicitly states a fresh click-through after recent scripts and a standalone build remained outstanding. Re-verify against the actual current editor before relying on those statements.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity Editor interaction | available through user's desktop UI when using computer-use workflow; current editor state not inspected in this onboarding | desktop capability; do not assume live state |
| Dedicated Unity MCP connection/scene inspector/test runner | unverified/unavailable in tool inventory for this task | no Unity-specific MCP tools exposed |
| Shell/file inspection | available | current workspace |
| Backend tests | available, not run this turn | pytest suite and local `.venv` directory |

## Important Constraints

- Preserve the dirty working tree and untracked project files; do not reset, clean, checkout, or overwrite scene/script changes.
- Keep Unity at `6000.6.2f1`; do not downgrade/reinstall.
- Do not claim a feature works from code presence alone. Verify in the currently open editor/Game view and distinguish runtime compile, visual behavior, input, backend, and build status.
- Museum and classroom share a scene; exhibit display is intended to be demand-driven. Avoid placing all classroom assets in the initial room without checking product intent.
- Voice uses local Windows facilities; speech recognition needs Windows microphone/speech privacy permissions. Speech output depends on installed system voices.
- Backend model calls can be slower than deterministic catalog/common-prompt routes; Ollama must run separately for actual Llama generation. No Gemini key is configured or required by current local-only design.
- Do not expose credentials; inspect local secrets only for presence/paths and never copy values into handoff documentation.

## Unknowns And Confidence

- Current status of Unity Editor compile after latest dirty edits and whether Play mode works: unknown; verify live.
- Whether current hub visual design looks correct and responsive across resolutions: unknown; current changes are uncommitted and need editor inspection.
- Café movement/order flow and NPC behavior after latest changes: require a fresh end-to-end test.
- Whether Ollama is running and `llama3.2:1b` is installed/fast on this machine: unknown at this inspection.
- Windows build target configuration and release build success: unknown.
- Provenance/license and exact presence of external Smithsonian models: inspect assets/attributions before release.
- README/completion-document details may be stale: root completion notes mention older test count/unfinished work while CHANGELOG reports a later verification pass. Treat as historical evidence, rerun tests and editor checks.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/GraphicsSettings.asset`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- `README.md`, `PROJECT_COMPLETION.md`, `CHANGELOG.md`
- Representative scripts: `Assets/Scripts/Config/AppConfig.cs`, `Assets/Scripts/API/AIClient.cs`, `Assets/Scripts/Runtime/ScenarioTransitionService.cs`, `Assets/Scripts/Runtime/WalkableHomeHub.cs`, `Assets/Scripts/Runtime/WorldExperienceDirector.cs`, `Assets/Scripts/Runtime/GroundingGuard.cs`, `Assets/Scripts/Runtime/ClassroomLearningExhibit.cs`, `Assets/Scripts/Runtime/MuseumGalleryExperience.cs`, `Assets/Scripts/Runtime/HomeScenarioPanel.cs`
- `embodied-ai-backend/README.md`, `requirements.txt`, `app.py`, `tests/test_app.py`
- `git status`, `git log -1`, build scene list, scene inventory, first-party script/assembly inventory

<!-- unity-onboarding:generated:end -->

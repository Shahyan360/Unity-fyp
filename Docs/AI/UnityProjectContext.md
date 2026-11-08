# Unity project context

The main checkout is at `D:\Lumen-Unity-Project`. Its pre-cleanup checkpoint is `37a3e2f` on `codex/lumen-front-page-hybrid-integration-20261106`. Earlier integration results and machine paths are recorded in the [November 6 integration report](FrontendHybridIntegration-2026-11-06.md).

## Runtime

- Unity 6000.6.2f1 uses URP, the Input System, uGUI with TextMesh Pro, and AI Navigation. glTFast and Draco import the models used by the exhibit prefabs.
- `HomeScene` is the first enabled scene. The remaining scenes are `Classroom`, `Cafestore`, and `HistoricalPlace`. `ScenarioTransitionService` maps destinations to these scenes. The museum is a variant of `Classroom`.
- `EmbodiedExperienceBootstrap` and `WorldExperienceDirector` initialize the player, world, guide, and UI. `LumenFrontPage` owns the startup menu and destination selection.
- `AIClient` sends requests to the local Flask backend at `http://127.0.0.1:5000`. Local conversation uses Ollama with `qwen3.5:2b-q4_K_M` by default. Hybrid mode uses the same local model and local transcription with Gemini text-to-speech. Full Gemini Live is a separate mode.
- The Unity scripts use `Assembly-CSharp` and `Assembly-CSharp-Editor`. Feature folders preserve the original script GUIDs and assembly boundaries.

## Working on the project

The [root README](../../README.md) maps each folder and lists the launch commands. World scripts and editor setup tools refer to the authored scene and asset paths. Before pruning an asset, check scene and prefab dependencies, dynamically loaded `Resources`, `StreamingAssets` inputs, shader includes, and editor tooling that loads files by path.

Use `LumenReleaseBuild.Build` for the Windows x64 player. The runtime verification routines are grouped under `Assets/Scripts/Validation`. Backend tests run with pytest from the project root. Unity Pipeline is available for existing editor tooling, including the [cast review scripts](../../Tools/Art/README.md).

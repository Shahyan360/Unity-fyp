# Embodied AI — World Experience Guide

## Included experiences

The build now opens at the Lumen world hub. Its three entry cards load the authored scenes through a short fade; the backend and Unity share the same scenario IDs.

| World | Unity scene | What you can do |
| --- | --- | --- |
| Ancient Rome | `HistoricalPlace` | Explore the Forum, Colosseum, temple and fountain context; ask about Roman civic life and architecture. An Egyptian object is clearly described as a comparison exhibit, not Roman. |
| Classroom | `Classroom` | Starts as a simple classroom. The guide builds only the requested exhibit from a 22-topic catalog: atoms, the Solar System, each planet, periodic table, plant/animal cells, DNA, heart, water cycle/molecule, circuit, volcano, Pythagorean theorem, plant, and light spectrum. |
| Café | `Cafestore` | Walk from eye height, talk with the barista or guests, and place/collect simulated pickup or table-service orders. Addressed people answer with distinct installed Windows voices; order/ready chimes are generated locally. No payment is taken. |
| Museum of Human Ideas | `Classroom` scene variant | A separate runtime-built gallery with eight labeled artifact replicas: Rosetta Stone, Antikythera Mechanism, Roman coin, Terracotta Warrior, Greek amphora, Venus de Milo, Sutton Hoo helmet, and Gutenberg press. Ask for an artifact to focus/highlight it and hear its explanation. |

The home guide can switch scenes when a person clearly asks to go, enter, open, or switch to Rome, the classroom, café, or museum. A question that merely mentions a place does not itself trigger a transition. Model/artifact requests are routed locally, shown on demand, and do not wait for Llama generation.

## Controls

- Use the hub cards to enter a world; the “Worlds” control returns to the hub.
- In a 3D world, the cursor starts free for UI. Use WASD to move; right-click on the world to enter mouse-look and press `Esc` to release the cursor. Left-click remains available for buttons.
- Look at an object or café person and press `E` to ask the guide about it.
- Use the chat panel for typed questions and the café menu button for orders.
- Hold `V` to dictate. On Windows this requires the system's speech-recognition privacy setting and microphone access to be enabled.
- The order lifecycle and menu prices are for practice only. There is no real checkout or payment integration.

## Run the project

1. Open this Unity project using the version recorded in `ProjectSettings/ProjectVersion.txt` (Unity `6000.6.2f1`).
2. Start Ollama with the configured Llama model and run the Flask API using [`embodied-ai-backend/README.md`](embodied-ai-backend/README.md). The default model is `llama3.2:1b`; a larger installed model can be selected with `OLLAMA_MODEL`. The browser root (`http://127.0.0.1:5000/`) describes the service; `/health` reports whether Ollama and the configured model are available.
3. Press Play. `HomeScene` is the first enabled Build Settings scene; all three experience scenes are also enabled.
4. If Ollama is unavailable or returns an unusable response, the backend marks the turn offline and uses the relevant curated local knowledge. Scene switching and classroom model requests use deterministic local routing.

## Implementation notes

- Scenario aliases from the earlier prototype (`academic_tutor`, `historical_guide`, `social_coach`) remain accepted, while the authored worlds use `classroom`, `ancient_rome`, and `cafe`.
- The hub and shared in-world UI are generated from Unity UI components so they adapt to resolution changes without manually editing each scene.
- The Rome, café, and classroom environments reuse their authored geometry. Classroom models and museum replica meshes are generated from Unity primitives at runtime; they are teaching visualizations, not photorealistic sourced assets. The guide reuses the Robot Kyle asset already in the project. No new third-party package was imported.
- The UI direction was informed by Unity's current Unity 6 guidance on scalable, component-based game UI. Representative optional assets reviewed were [University Classroom](https://assetstore.unity.com/packages/3d/props/interior/university-classroom-86107), [Mega Roman-Greek City Pack](https://assetstore.unity.com/packages/3d/environments/historic/mega-roman-greek-city-pack-125443), and [Coffee Shop Environment](https://assetstore.unity.com/packages/3d/environments/coffee-shop-environment-217600); these were not imported. Check current publisher terms and pipeline compatibility before adopting any Asset Store package.

## Checks

Run the backend tests from `embodied-ai-backend`:

```powershell
.\.venv\Scripts\python.exe -m pytest -q -p no:cacheprovider
```

The current suite passes 14 tests, including the local API landing/health routes, session/chat contract, explicit scene switching, museum and planet/model routing, fast no-Ollama answers for stored knowledge and common daily prompts, malformed-model fallback, and protection against unrequested scene changes. A live local Llama request also returned a response (about 6.9 seconds in the measured open-ended sample); greetings, common day-planning, curated Rome/café answers, and model/artifact requests tested in about 1–3 ms through the backend fast paths.

Unity `6000.6.2f1` is open on the existing café scene. The café's authored camera was found at an aerial-review offset; the runtime controller now resets it to eye height and preserves ground-level WASD movement with right-click mouse-look. These latest C# changes still need a fresh Unity script reload and Play-mode click-through. The editor is currently in use, so I did not force-stop it. A standalone Windows build and full end-to-end visual/audio verification are not included.

## Not yet production-complete

- The experience uses existing authored scenes, runtime UI, and primitive-based learning/museum exhibits. Full cinematic camera rigs, authored quest/codex/quiz progression, advanced effects, layered audio mixing, and Addressables are not implemented.
- The guide reuses the project's Robot Kyle model/animator with procedural idle/gesture motion; custom blend trees, IK, facial expressions, and lip-sync are not implemented.
- Offline Windows text-to-speech uses voices already installed on this PC (Microsoft David and Zira). No additional voice packs were downloaded. Café voice replies are event-driven when a person is addressed, not continuous background chatter.
- Windows push-to-talk is wired, but dictation failed on this PC because Windows speech recognition privacy/device access is disabled. Enable it manually in Windows Settings before expecting `V` voice input to work.
- The café is a simulated practice order flow only; it has no payment, inventory, or external POS integration.
- The backend process already running on port 5000 is an older process and must be stopped/restarted to load the updated API and museum route. Exit Play mode or refresh/reopen the same Unity 6.6 project so Unity imports the latest scripts; then verify Home → worlds → café, voice output, and the new museum/classroom displays.
- The project has not been built and tested as a standalone Windows release, mobile build, or tested against a sustained 60-FPS target. `dotnet build` could not compile Unity's generated project because the Windows .NET Framework 4.7.1 targeting pack is not installed; Unity's own script compilation still needs to be confirmed after refresh.

# Embodied AI - Completion Guide

## Delivered project behaviour

The Unity application now works with a self-contained local backend in `embodied-ai-backend`. It retains the original home and 3D demonstration scenes and upgrades them at runtime through the existing `AIClient`, avoiding fragile manual scene rewiring.

| Requirement | Delivered implementation |
| --- | --- |
| Local AI bridge | Flask API with Ollama / Llama configuration, health check, robust JSON parsing, offline diagnostic fallback, and Unity-compatible response tags. |
| Three scenario modes | Academic Tutor, Historical Guide, and Social Coach persist through scenario selection and alter the active persona, RAG evidence, ambience, and object context. |
| Contextual awareness | Camera raycasting passes selected `ObjectMetadata` to the backend, which prioritises it in its answer and selects a pointing action. |
| Navigation | Companion following retains an offset and safely samples NavMesh; an AI response can also target a named metadata object. |
| Social practice | The backend starts interview, presentation, or conversational practice with a prompt and uses a constructive feedback contract. |
| Expression | Structured emotion and action tags drive agent animation triggers and a visible light-based emotional state. |
| Speech input | Windows standalone builds support hold-`V` dictation, sent through the same chat flow as typed input. |
| Privacy | Flask binds to localhost and session history lives only in memory. No cloud model or external analytics is required. |

## Run it

1. Start Ollama and run `ollama pull llama3.2:3b`. For a 4 GB VRAM device, use `llama3.2:1b` instead and set `OLLAMA_MODEL=llama3.2:1b` before starting the backend.
2. In a PowerShell window, run the commands in [`embodied-ai-backend/README.md`](embodied-ai-backend/README.md).
3. Open the Unity project in Unity 6. The existing home page selects a scenario; its demo scene automatically receives the completed runtime systems.
4. Look at a tagged object before asking a question to demonstrate Digital Sight. Press and hold `V` for Windows dictation.

## Test evidence

Run `python -m pytest -q tests` from `embodied-ai-backend`. The tests validate the local health and scenario catalog, session lifecycle, Unity response contract, offline fallback, and social-coaching prompt. Unity compilation was checked in Unity 6 with no C# errors.

## Optional asset expansion

The included project assets already support the academic room, historical exhibit, and social-practice area in the demonstration shell. To turn each module into an entirely separate high-fidelity level, import assets through the Unity Package Manager under the asset publisher's licence. A current small lab option is [Sci-Fi Lab Kit](https://assetstore.unity.com/packages/3d/environments/sci-fi/sci-fi-lab-kit-modular-stylized-low-poly-environment-assets-324212); the [Unity Asset Store free-assets collection](https://assetstore.unity.com/top-assets/top-free) is the safest place to obtain additional compatible art.

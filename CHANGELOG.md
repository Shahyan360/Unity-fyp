# Changelog

## 2026-10-13 — Unity 6.6 integration and QA

- Kept the existing Unity 6.6 project version and built on its authored Rome, classroom, and café scenes.
- Added the Lumen hub, world cards, shared HUD/chat, fades between scenes, object/person knowledge prompts, classroom atom/Solar System/plant-cell exhibits, and a simulated café menu/order/collect loop.
- Added first-person Input System navigation to Rome and the classroom, used a Forum-entry spawn for Rome, and made the hub handle both `HomeScene` and the loaded `HomeScene 1` copy.
- Corrected push-to-talk input for the active Input System and replaced obsolete Unity object-finding calls in project scripts.
- Constrained the local Ollama response to a JSON schema, set the installed `llama3.2:1b` as the default, and added curated-knowledge fallback for malformed or unavailable model replies. Scene changes remain explicit user-intent routes.
- Added concise, accurate deterministic explanations for the atom, Solar System, and plant-cell requests, even when the LLM is offline; verified with the expanded 9-test suite and a live Solar System API request.
- Corrected 3D classroom text facing and caption size after checking the scene in Unity. This follow-up change awaits a fresh visual Play-mode check. The hub-to-Rome/return and classroom entry/model rendering were exercised in Unity 6.6. Café order interaction and standalone build remain unverified.
- Decision: reuse the existing environment assets and avoid unreviewed third-party imports; generate UI and teaching models at runtime to preserve the project's visual style and reduce licensing/storage risk.

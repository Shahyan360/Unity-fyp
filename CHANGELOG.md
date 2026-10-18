# Changelog

## 2026-10-16 — Approved Roman city restoration, first implementation slice

- Removed the rejected Roman-gallery override and restored the authored full-sized city, forum, temples, shops and amphitheatre as the playable Rome route.
- Created and inspected a full-sized Blender market stall and a 120-arch retrofit for the retained amphitheatre. Integrated four stalls with counter/post collision and navigation carving. Replaced obsolete black opening rectangles with actual stone arches.
- Retired primitive crowd spawning; reused eight imported humanoid visitors as an explicitly interim population pending Roman clothing.
- Inspected actual Unity city/market captures, tested real movement against the stall counter, observed Marcus navigation, and confirmed a clear current runtime Console. Full environment plan and exact continuation checkpoint saved in Docs/AI/ApprovedEnvironmentPlan.md and PROJECT_PROGRESS.md. Museum redesign and broader city finishing remain pending.

## 2026-10-16 — Compact museum and Roman visual pass

- Added a small classical museum entrance with a level doorway, planted reflecting pools, original stone urns, copper pediment roof and lamps. Retained museum exhibits and added original fossil impressions and a Roman tabletop display.
- Replaced the runtime outdoor Roman prototype view with a 12 × 10 m exhibition gallery, reusing Marcus, the existing temple study and new Blender-authored city miniature/pottery cabinet. Preserved the authored city on disk and excluded its hidden landmarks from guide navigation.
- Inspected all four new Blender renders and real Unity Game View captures. Verified fresh scene routes, conversation open/hide, café menu, classroom dust fix, player collision and Marcus navigation to the new Forum exhibit. Final runtime Console ground truth was clear; this does not validate a new standalone build.
- Archived four unreferenced duplicate/demo scenes with metadata and SHA-256 receipts before removing them. New references, editable art, captures and archives are consolidated in D:/LumenProject. Ollama/backend work is deferred per user direction.

## 2026-10-16 — Recovered character/Blender integration and runtime verification

- Confirmed the interrupted character and prop builders completed; preserved the three Rocketbox host prefabs and Blender collection rather than recreating them. Added provenance and retained source/render artifacts on D.
- Exercised Home, Classroom, Café, Rome and museum scene routes in Play Mode, Unity-to-Flask persona chat, requested Solar System/Rosetta Stone behavior, and the café add/place/prepare/collect button flow. Backend: 37 tests passed in 8.95 seconds.
- Corrected imported-character component cleanup, conversational facing, speaking-status overflow, Home font glyph, café lighting/menu contrast/modal layering and quick-prompt readability. Reduced oversized classroom dust. Added explicit session reset/background operation for repeated Editor runs.
- Recorded actual speech, jaw and blink motion for Ada, Maya and Marcus; measured stationary foot support and moving Maya NavMesh/foot alignment. Confirmed upper-deck placement/following; full stair climb and mouse-look locking remain unverified. Reports/screenshots: D:/LumenArtifacts/Verification.
- Started a Windows build on D with all existing enabled scenes. Consult PROJECT_PROGRESS.md and the build-result artifact for its current verdict; no release-complete claim is implied.
- Created PROJECT_PROGRESS.md as the authoritative resume log, with pending product work, current defects, tool paths and exact next actions.

## 2026-10-14 — Grounding safety net + full verification pass

- **Priority 1 (grounding): added a new, purely-additive `Assets/Scripts/Runtime/GroundingGuard.cs`.** It is a scene-wide, once-per-second watchdog that only ever lifts an object back up when it has fallen more than 2.5 m below its real floor (raycast/physics, falling back to the baked NavMesh). It never pushes anything down and never touches objects that are already grounded, so it cannot affect normal walking, stairs, the café upper floor, or the guide/crowd NavMeshAgents. It installs itself automatically in every scene and logs each recovery.
- **Fixed a player-bounce regression in `GroundingGuard`**: its floor-finder cast from 40 m above and took the first hit, which in the two-floor café is the upper deck/ceiling (~3.6 m), so it wrongly judged the player to be "below the floor" and teleported them up, then they fell and it repeated. `GroundingGuard` now only ever looks at the surface directly *at/just below the feet* (ignoring all overhead geometry) and only rescues a genuine fall into a void where no floor exists under the feet. It can no longer lift a correctly-grounded player.
- **Priority 1 (characters sinking/lying on the floor): added `Assets/Scripts/Runtime/CharacterGrounding.cs`.** The Quaternius café guests/Rome visitors and the Robot-Kyle guide are anchored to the NavMesh/seat height, which can sit slightly above or below the visible deck (worst in the two-floor café), so their feet sank under the floor. This component measures each character's foot height once and, every frame, nudges only the VISUAL mesh so its feet sit exactly on the real physics floor (navmesh/seat may move the container horizontally, but this never moves the NavMeshAgent, so it cannot fight navigation). It ignores triggers and furniture above the body, is a no-op when no floor is found, and falls back to the NavMesh during transitions. Wired into café guests (`WorldCharacterCrowds.SetupCafePerson`), Rome time-travel visitors (`SpawnImportedVisitor`), and the guide (`WorldExperienceDirector.EnsureGuideCharacter`).
- Audited every scene's grounding: all players use `CharacterController` + one-time ground snap + gravity + fall recovery; all guide/crowd characters use `NavMeshAgent` + Warp-on-recovery + `baseOffset 0` + `updateUpAxis` + `applyRootMotion=false`. Confirmed the runtime disables the competing `ClassroomWaypointNavigator` and `CafeFollowPlayer` so the companion controller is the single destination writer. Confirmed no character prefab has a gravity `Rigidbody`. No movement scripts were edited (they were already correct); the guard is defense-in-depth.
- Verified the project **compiles cleanly under Unity 6000.6.2f1** (Editor.log: "Tundra build success / successfully reloaded assembly"; no CS errors). Independently compiled `GroundingGuard.cs` against Unity's reference assemblies + `Assembly-CSharp` to prove API/syntax validity without touching the open editor.
- Verified the Flask backend: **26/26 pytest pass**, and live `/`, `/health`, and six representative `/chat` routes returned the correct JSON contract (small talk, `show Mars`→`navigation_target:"Mars"`, classroom catalog, museum `Rosetta Stone`, `take me to the cafe`→`next_scenario:"cafe"`, and café-asked `Antikythera`→`next_scenario:"museum"` carry-through).
- Confirmed the recurring "Account API did not become accessible within 30 seconds" Console warning originates from the `com.unity.ai.assistant` Unity-cloud package (HTTP 403/404 on a Personal licence) — it is unrelated network/account noise and does not block any feature.
- Decision: reuse existing authored scenes/runtime builder and keep the guard upward-only; no third-party package added; Unity version unchanged.

## 2026-10-13 — Unity 6.6 integration and QA

- Kept the existing Unity 6.6 project version and built on its authored Rome, classroom, and café scenes.
- Added the Lumen hub, world cards, shared HUD/chat, fades between scenes, object/person knowledge prompts, classroom atom/Solar System/plant-cell exhibits, and a simulated café menu/order/collect loop.
- Added first-person Input System navigation to Rome and the classroom, used a Forum-entry spawn for Rome, and made the hub handle both `HomeScene` and the loaded `HomeScene 1` copy.
- Corrected push-to-talk input for the active Input System and replaced obsolete Unity object-finding calls in project scripts.
- Constrained the local Ollama response to a JSON schema, set the installed `llama3.2:1b` as the default, and added curated-knowledge fallback for malformed or unavailable model replies. Scene changes remain explicit user-intent routes.
- Added concise, accurate deterministic explanations for the atom, Solar System, and plant-cell requests, even when the LLM is offline; verified with the expanded 9-test suite and a live Solar System API request.
- Corrected 3D classroom text facing and caption size after checking the scene in Unity. This follow-up change awaits a fresh visual Play-mode check. The hub-to-Rome/return and classroom entry/model rendering were exercised in Unity 6.6. Café order interaction and standalone build remain unverified.
- Decision: reuse the existing environment assets and avoid unreviewed third-party imports; generate UI and teaching models at runtime to preserve the project's visual style and reduce licensing/storage risk.

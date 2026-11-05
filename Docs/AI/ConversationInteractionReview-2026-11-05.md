# Conversation and interaction branch review — 5 November 2026
> Subsequent hybrid routing and voice validation are documented in [HybridVoice-2026-11-05.md](HybridVoice-2026-11-05.md).

Reviewed `codex/lumen-conversation-interaction-fixes-20261105`, pulled from origin at `6fbe555`. The branch was already current after the pull. Review covered its two commits after `9242bd4`, their immediate callers, backend packaging, and existing validation entry points. The pre-existing TMP font and EditorBuildSettings edits were preserved.

## Corrections

| Priority | Finding and correction | Evidence |
| --- | --- | --- |
| High | The release packager omitted `neural_voice.py`, although every backend startup imports it. Added the module to staged releases. | Reproduced `ModuleNotFoundError: No module named 'neural_voice'` using the original staging list; isolated staged-service startup now passes. |
| Medium | Releases omitted the local knowledge files and fitted guide retriever. Package the knowledge directory with the backend. | Staged-service test resolves the lunar-phases reference outside the repository. |
| Medium | Invalid or incomplete optional Piper configuration raised an exception during Flask startup, disabling text conversation too. Validate the speaker map and leave neural speech unavailable when installation is damaged. | Regression tests cover truncated JSON, a missing speaker map, and an invalid map; ordinary chat remains available. |
| Medium | Local streamed answers about an object could inherit the previously selected NPC's transcript label. Limit live-partner reassignment to replies explicitly sourced from `gemini-live`. | Targeted Unity regression exercises a generated object answer while an NPC remains selected. |
| Medium | Addressing a switched guide by name omitted its character ID and could select the world's default persona. Send the active guide ID when its display name is the addressed object. | Unity regression switches the guide and checks the identity returned through the real AIClient request. |
| Medium | After a streaming error, the next turn could overwrite the previous partial answer. Reset the completed/error turn's transcript index. | Unity regression checks that both partial and subsequent reply text survive. |
| Low | The local benchmark rejected valid `local-reference` fallback answers. Accept this existing provenance value. | Real backend benchmark completed; fallback behavior is covered by backend tests. |

## Validation

- Unity 6000.6.2f1 batch compilation passed before remediation; the corrected scripts also compiled during subsequent validation/builds.
- The original Unity interaction journey passed **48 checks**: all four destinations, gaze identification, available NPC follow-ups, real atom-image download, and image continuity through automatic world travel. The final expanded journey passed **51 checks with zero failures**, including streamed-object attribution, failed-turn transcript preservation, and addressing a switched guide by name. Results are recorded in `Builds/BranchReview-20261105/verified-interactions.json`.
- **112 tests passed**, including the complete backend suite, isolated staged-backend startup, missing-player packaging guard, and real interpreter relocation into a directory containing spaces. Command: `D:\LumenTools\VoiceRuntime\Scripts\python.exe -m pytest -q -p no:cacheprovider embodied-ai-backend/tests Tools/tests/test_package_release.py`.
- The checkout's C: virtual environment passes the backend and lightweight packaging checks but lacks `google-genai`, so the full runtime-relocation test fails there. The configured D: voice runtime passes the entire suite; no dependency was hidden or test disabled.
- Five real local backend turns completed: four model answers plus explicit preference recall. Generated replies completed in approximately 1.94–2.50 seconds in this small sample. This is not a general latency or answer-quality guarantee.
- Real Piper synthesis with the installed D: voice files produced nonempty mono, 22050 Hz WAV output for `p226` and `p229`, taking approximately 0.85 and 0.90 seconds after warmup. This does not measure microphone-to-speaker latency.
- Final Windows x86_64 build succeeded with the guide-identity correction included: **zero errors**, 698,874,276 bytes, build GUID `85cc21cea5d942aebd678ea541d8247e`, approximately 3m34s. Artifact: `Builds/BranchReview-20261105/Windows-Final/Lumen.exe`. The earlier build and startup log are retained separately.
- Build warnings: 500 package shader warnings, one warning about three meshes with collision pre-baking disabled, and one optional Pipeline runtime configuration warning. These originate in unchanged packages/assets; no package or scene rewrite was performed to suppress them.
- A standalone startup smoke check initialized Direct3D 12 on the NVIDIA T500, input, and scene configuration using isolated preferences (`player-smoke.log`). The owned player process was then terminated. This was a startup check, not a standalone navigation or visual acceptance journey.

## Limits

Natural microphone input, acoustic echo/barge-in, Gemini/hybrid provider sessions, perceived speech quality, visual layout at different resolutions, and a fresh performance/long-duration soak remain unverified in this review. No new full distributable archive was staged. The source fixes, backend tests, build result, and recorded scene checks do not establish universal feature or release acceptance.

Raw local evidence is under ignored `Builds/BranchReview-20261105/`. Changes remain local and uncommitted; nothing was pushed.

The original font and build-settings files were restored byte-for-byte after Unity imports; generated build-only settings/resources were removed. Temporary test backend and project Ollama services were stopped. The pre-existing Ollama service on port 11434 was preserved.

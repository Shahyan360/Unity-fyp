# Lumen Windows acceptance — 22 October 2026

This private Windows acceptance candidate has completed the automated checks
listed below, including actual portable-package runs. Human audio/device checks,
performance targets and distribution evidence remain open; this is not a public
release or a claim that every acceptance requirement is closed.

## What changed

Kai's upper trousers were tucked inside his coat, Aurelius's upper trousers
inside his tunic, and Maya's remaining white cap triangles were removed. Maya's
existing dark crown was extended after the first review exposed a seam. Original
mesh GUIDs, rig weights, bind poses, UVs, normals and all 175 facial morphs were
preserved. The correction recipe and its baseline hashes are tracked in
`Tools/Art`; no external cap mask is required.

Visual review also found Museum's header incorrectly reading CLASSROOM. The
virtual Museum scene name is now recognized, and the functional harness checks
both Museum and Classroom headers. Character acceptance now captures walking
from the front, side, rear and three-quarter view, plus front/rear pointing,
speaking and silence.

The build, packaging and authoring tools use discovered or project-relative
paths. Packaging includes a relocated Python runtime, installed dependencies,
notices and a launcher that quotes arguments and stops only its own service.
The soak records resource counts without forcing garbage collection. Missing
render-recorder samples are recorded as unavailable instead of zero.

## Candidate identity

- Unity 6000.6.2f1, Windows x64, Direct3D 11 acceptance runs.
- Build GUID: `ce3ac243a7174daab6c13cdaa44a4763`.
- Player implementation checkpoint: `8e00c9ff86222b1742a27a5f5cbfbcf00f0d2852`.
- Backend corrections: `9a7498f`, `50ca648` and `a51faac`; the player binary is unchanged.
- Handoff branch: `codex/lumen-release-validation-20261022`, in
  `https://github.com/Shahyan360/Unity-fyp`. The branch tip is the final
  documentation/source checkpoint; it must not be confused with the earlier
  player build checkpoint.
- Four scenes provide Home, Rome, Classroom, Museum and Café.
- Build directory: `Builds/Windows-Candidate-20261022-03`.
- Private portable directory: `Builds/Portable Acceptance/Lumen-20261022-03`.
- The build was made before the final documentation commit. Its source receipt
  retains the generated TMP fallback-font cache overlay separately; it is not
  a claim of a clean worktree. Runtime scripts/configuration match the checkpoint. The generated fallback
  glyph is now committed too: the committed Unity-input diff matches the saved
  build overlay byte for byte.

The build succeeded in 2m 58s with one RenderTexture zero-height error and two
warnings: absent Pipeline runtime configuration and uncompiled Editor changes.
Completed Editor compilation and the stale-source guard preceded the build.
Final runtime checks verified the new header and capture behavior. The warning
is retained rather than treating this as a zero-warning build. The cached build's
packed-asset report is header-only; the earlier candidate's detailed inventory
is retained as supporting evidence, not an exact current asset ledger.

## Corrected-player checks

| Check | Result |
| --- | --- |
| Functional behavior and responsive layouts | 276 passed; zero errors and detected overflows |
| Six guides, walking angles, gesture, speech and silence | 269 passed; 70 captured views; zero errors |
| Conversation before backend correction | 69 checks; one failed: provider refused Atom with images disabled |
| Conversation after backend corrections | 69 passed; zero errors |
| Real-provider walkthrough | 55 checks; 14 questions over 616.58 seconds; owned-backend restart; zero errors |
| Offline world-transition stability | 30 cycles; zero errors; memory/resource analysis completed |
| Portable functional launch from an external folder with spaces | 276 checks; zero errors/overflows; launcher cleanup passed |
| Portable real Gemini conversation | 69 checks; zero errors; launcher cleanup passed |
| Portable live images after relevance correction | 73 checks; zero errors; launcher cleanup passed |

The conversation failure reproduced an ambiguous image policy: the provider
treated images-off as disabling local 3D exhibits. The backend now distinguishes
retrieved 2D images from requested local models. Its 86-test suite passed after
the change. The live walkthrough then displayed Atom and DNA with images off;
the broader conversation retry is recorded separately below.
The failed run is preserved under `conversation-03`; `conversation-03-policy-retry`
passed all 69 checks after both backend policy corrections.

The first image run (`images-03`) displayed relevant electrical-transformer and
AI-transformer images, Eiffel Tower, DNA, photosynthesis and Triceratops with
credits. Visual inspection confirmed that the Philips source filename refers
to a power-supply transformer, not an unrelated complete tape recorder. However,
the provider incorrectly carried a one-turn verbal-only request into the next
explicit Colosseum picture request, and the run reached its bounded deadline
after an approximately four-minute pause. That run is failed, not accepted. Backend checkpoint `50ca648` limits an ordinary
verbal-only refusal to its intended turn; a later explicit image request can
proceed when the image preference allows it. Its 86-test suite passed.

The resumed image run completed all 73 checks with one relevance failure:
a verbose DNA-model request selected Rosalind Franklin's portrait. Image lookup
now strips presentation instructions and requires topical title overlap before
using either a network result or a cached result. All 89 backend tests passed,
including the new regression cases; an uncached real request returned a DNA
structure image. Final portable-player image acceptance passed all 73 checks with zero errors;
the corrected DNA image and final image captures were reviewed.
The failed and interrupted image receipts remain preserved.

## Stability and performance

All 30 offline destination/Home cycles completed without forced collection.
Post-warmup asset, audio and controller counts were stable. Comparing cycles
8–12 with 26–30, Unity allocated memory changed by -0.08 MiB and Windows private
bytes by +1.04 MiB. Managed usage rose 1.39 MiB and handles averaged +7.8;
these bounded observations do not prove absence of leaks. The soak does not
exercise 30 online connection cycles. The separate real-provider walkthrough
covers its own connections, cancellation and restart.

Measured steady Unity frame-interval p95 ranged from 41.91 ms at Home to
73.26 ms in Museum at 1600×900 Balanced on Intel UHD. A 1206.50 ms Museum
interval remains an unresolved tail event. This is not a performance-budget
pass; the original protocol and numerical targets were missing. See
`PerformanceFindings-2026-10-22.md` for definitions, CPU/GPU timings, counters,
memory comparisons and remaining profiling work.

## Existing fresh supporting checks

- Backend after the image-relevance correction: 89 tests passed; the earlier
  86-test checkpoints and clean dependency check remain preserved.
- Final packaging checks: two tests passed in 106.39 seconds, including real relocated interpreter imports
  from a path containing spaces. The original long-path failure is preserved.
- First staged package launcher: argument quoting, owned-service shutdown,
  occupied-port refusal and preservation of the unrelated service all passed.
- Four repaired mesh stream comparisons passed. Static four-angle reviews of
  all six characters and valid before/after captures are retained.
- Superseded build `750b464c7b2d4371b8131217c95c7c65`: 273 functional, 197
  character and 69 conversation checks passed. These are supporting evidence;
  they do not replace the corrected build's final receipts.

## Practical limits and remaining requirements

Natural microphone accuracy, noisy-room recognition, speaker echo, audible voice
quality, physical device removal and reconnect need a person and the intended
devices. Automated typed and synthetic speech checks remain separate. Mouth
motion uses audio envelope/spectral features; it is not phoneme aligned.

This machine has Windows 11, an i5-10210U, about 8 GB RAM and Intel UHD graphics.
The original performance protocol and numerical targets were not transferred.
Measurements report Update callback gaps, Unity frame intervals, CPU and GPU
timings separately; unavailable counters do not establish low rendering cost.
The Editor and the user's ordinary applications remain open on this computer.

Offline navigation, exhibits and curated text are included. No Whisper model or
Piper/eSpeak runtime is bundled. Offline speech recognition needs those optional
dependencies; speech output can use installed Windows voices. This computer has
Microsoft David and Zira, which does not prove voice availability on another PC.

The original audit, performance protocol, complete shipped-asset ledger, external
art reports, old native Blender files and original download receipts were not
transferred. No original audit issue IDs or rights clearances are invented.
The current issue matrix is derived from the user's continuation brief.

Liberation Sans OFL, Quaternius CC0, Rocketbox MIT and UnityTechnologies notices
are included. Current Met API public-domain flags were verified for all three
paintings. Remaining legacy asset/runtime provenance and public redistribution
requirements are recorded in `DistributionLedger-2026-10-22.md`. Testing under
another Windows user and on another machine remains outstanding.

Use `ReleaseRunbook.md` for reproduction and `PackageStartHere.md` for launch.
Raw failed runs and superseded builds remain under `Builds/Finalization-20261022`;
temporary captures, caches and credentials are excluded from source commits.


## Handoff evidence

`ReleaseEvidence-2026-10-22.json` contains compact counts, source identities,
raw-receipt hashes, memory comparisons and timing definitions without visitor
conversation transcripts. The portable folder includes `ACCEPTANCE_REPORT.md`,
`PERFORMANCE_FINDINGS.md`, `Evidence/ReleaseEvidence.json`, runtime/asset notices,
source/build receipts, `HANDOFF_CHECKPOINT.json` and a verified
`PACKAGE_SHA256.txt`. The final documentation commit is identified by the new
branch tip and that handoff receipt, while earlier build/backend commits remain
explicitly identified above.

The offline package check began before the image-only relevance update; the
subsequent online and image checks used the updated module. Shared offline
behavior, the player binary and scene inputs were unchanged. All three actual
launcher runs exited successfully and stopped only their owned backend. Port
conflict and argument-quoting fixtures are recorded separately from game results.
The package inherits this Windows user's configured Gemini key and installed
Windows voices; it does not establish clean-user portability.

The local before/after and runtime character gallery is
`Builds/Finalization-20261022/CharacterGallery-Final.html`. Raw captures, failed
runs, recovery data and build outputs are deliberately excluded from the source
push. The complete playable folder remains local under
`Builds/Portable Acceptance/Lumen-20261022-03`; copy the entire folder and run
`Start-Lumen.cmd`. Public binary publishing and merging the branch are separate
actions and have not been performed.

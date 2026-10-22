# Lumen release continuation — 22 October 2026

This is the current-machine record. The reports dated 17–19 October describe
another computer. They are retained as historical evidence, not current passes.
The application must not be called finished until the current build, package,
and acceptance receipts agree.

## Recovered state

- Repository: `https://github.com/Shahyan360/Unity-fyp`, branch
  `codex/lumen-finalization-20261020`, starting commit
  `f0c57017dd20856257ee34a7e55db722898609d6`.
- Project and installed Editor: Unity **6000.6.2f1**, URP 17.6.0. No Editor or
  package upgrade was performed.
- Initial local setup edits, URP/TMP edits and serialized settings were preserved
  in `Builds/Finalization-20261022/starting-worktree.patch`; starting untracked
  setup files were copied beside it. No reset or forced checkout was used.
- Windows 11 Pro 10.0.26200, Intel Core i5-10210U, 8,204,566,528 bytes RAM,
  Intel UHD Graphics driver 31.0.101.2134, desktop 1920×1080. Runtime telemetry
  is authoritative for the GPU, graphics API, quality and applied render size.
- Python 3.12.15 is available in the ignored backend environment. No Blender
  executable was found on PATH or in its standard Program Files installation.
  Existing Unity mesh tools were used for the character repairs.
- An existing HomeScene Editor held the project lock. Initial batch attempts
  exited before loading the project; these were not successful builds. Pipeline
  subsequently connected on port 7800. Compilation and stopped Play mode were
  checked before issuing the build through that Editor.

## Missing transferred material

The original professional audit, issue-resolution matrix, performance protocol,
shipped-asset ledger, continuation/checkpoint report set, original baseline
captures and recent Windows packages are absent. The source references
`Reports/IMPLEMENTATION_BRIEF.md` outside the repository, but that file was not
transferred. Original audit issue numbers therefore cannot be reconstructed.

The previous computer's `D:/LumenProject/Art/CharacterTailoring` references,
corrected export jobs, turntables, native `.blend` files, external Maya masks,
original download receipts and `D:/LumenProject/References` material are absent.
Tracked dressed prefabs, weighted mesh assets, native Rocketbox FBXs/textures,
the Rocketbox MIT notice, and authoring scripts are available. New evidence is
stored locally under ignored `Builds/Finalization-20261022`.

## Implemented in this continuation

1. Four-angle previews reproduced the three outstanding character defects.
   Kai's hidden upper trousers and Aurelius's hidden upper trousers were tucked
   inside the existing garments. No enlarged garment shell was introduced.
   Maya's remaining 26 cap-island triangles were removed; the existing weighted
   dark crown was extended to close the exposed seam. A first correction review
   exposed that seam and remains preserved under `Cast-Corrected`.
2. Four imported mesh assets were updated in place. Their GUIDs were preserved.
   All six inspected body meshes retain 80 bones and 175 facial blendshapes.
   Mesh stream inventories and four-angle images are in `Cast-Baseline` and
   `Cast-Final`. Static preview does not by itself accept all animated poses.
3. `Tools/Art/CorrectCast.cs` records the guarded correction recipe;
   `cast-correction-baseline.json` contains original hashes and the small cap UV
   mask. It refuses a changed baseline or repeated application. Source FBXs,
   source textures and animation assets were not replaced.
4. Character authoring uses `LUMEN_ART_ROOT`, defaulting to ignored
   `Builds/Art/CharacterTailoring`. Maya's mask and crown generation no longer
   require external mask data. Full Blender regeneration remains untested here.
5. `LumenReleaseBuild.Build` builds four explicit release scenes into a new
   directory, emits a build GUID, warnings/errors and packed-asset residency,
   and restores preloaded assets. Existing build directories are preserved.
6. The soak no longer forces collection or explicitly unloads assets before
   measuring. It now also inventories persistent objects, AudioSources, playing
   sources and live controller counts. These counts do not directly enumerate
   operating-system socket handles.
7. Render recorders without samples produce `-1`. Telemetry closes and flushes
   on application quit and destruction through an idempotent close path.
8. Packaging requires explicit existing build/output paths. It copies an
   installed Python dependency closure and an explicit backend source allowlist,
   includes no user sessions or credentials, and treats absent offline models
   and Piper as optional. Google pure Python modules are zipped to avoid a
   reproduced Windows path-length failure. Player arguments containing spaces
   are quoted by the launcher. The installed Gemini SDK is pinned to 2.32.0.

## Fresh checks

- Backend suite: **86 passed**, 7.60 s; dependency check: no broken requirements.
  Sandbox-only first attempt had five temporary-directory fixture errors;
  rerun outside the sandbox passed without changing tests.
- Real Gemini authentication and a direct `gemini-3.8-live` audio response
  succeeded in this chat. This was a provider check, not final player acceptance.
- Packaging tests: **2 passed**, 82.06 s. This verifies relocated interpreter
  imports and rejection of missing player builds, not a complete package launch.
  The initial failing long-path test XML is retained separately.
- Authoring and packaging Python files parsed/compiled; launcher syntax parsed.
- Final player, performance, image, live, soak and package results must be read
  from their new receipts. Do not substitute the historical 273/36-question
  results for them.

## Acceptance still requires evidence

Natural microphone recognition in quiet/noisy rooms, audible voice quality,
echo/barge-in with real speakers, and audio-device removal require a person and
appropriate devices. Automated typed input and synthetic audio are reported
separately. Facial motion remains spectral/envelope driven, not phoneme aligned.

No transferred numerical performance target was available. Record per-world
engine interval, CPU time, GPU time and Update callback gap independently;
report p95/p99 and stalls without inventing a passing budget.

Public redistribution is not authorized by a successful source push or private
package test. See `DistributionLedger-2026-10-22.md` for unresolved notices.

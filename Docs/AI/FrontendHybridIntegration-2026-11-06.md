# Frontend and local hybrid integration — 6 November 2026

Working branch: `codex/lumen-front-page-hybrid-integration-20261106`.

The frontend branch was fetched from origin at `29506fa` (`codex/embodied-ai-front-page-20261106`). Its common ancestor with the local conversation branch is `6fbe555`.

Before merging, all local tracked edits and new source files were committed as `4d1e256` on `codex/lumen-local-hybrid-backup-20261106`. An additional exact working-file backup, SHA-256 manifest and binary Git patch are retained in ignored `Builds/Integration-20261106/`. No reset, discarded local edit or remote history rewrite was used.

## Resolution

- The runtime director automatically merged both the incoming `LumenFrontPage.Install()` and the local single-speech-controller setup.
- The TMP fallback cache conflicted. The incoming complete asset was retained atomically because its character table includes the local downward-triangle glyph plus the frontend's extra symbols. Mixing texture data and glyph rectangles from different atlases would be unsafe. The complete original local asset remains recoverable from the backup branch/archive.
- Project context notes conflicted. Both the hybrid-voice context and frontend implementation/validation notes were retained, with a new integration header identifying the actual local checkout.
- The existing opt-in hybrid verification journey now also exercises the merged front page before entering the courtyard. It checks chooser/atlas separation, atlas geometry, all three imported previews, shared settings and restoration of the existing conversation flow.

Source-preservation checks found no unexpected differences: 27 local-only files and 22 frontend-only files match their respective source commits exactly. The merged font matches the incoming asset and contains all local cached characters. The shared runtime director retains both changes; the project notes and validation harness contain the intentional integration updates. The backend, speech implementation and launchers match the preserved local checkpoint.

## Validation

- Backend/packaging suite: **122 passed** with the configured D: voice runtime, including staged hybrid startup and relocated interpreter checks.
- Unity 6000.6.2f1 Windows build: **Succeeded, zero errors**, GUID `5cbc4e5526de479185f64937d1f61f12`, 699,958,525 bytes. Existing package/asset warnings remain (502).
- Standalone integration journey: **24 passed, zero failures**. New UI, existing four courtyard destinations, one speech controller, local typed/spoken answers, Gemini audio, guide voice selection and cancellation all passed together.
- Rendered front-page and atlas captures were visually checked at 1280×720. This is not a new full resolution/device/performance acceptance pass.
- Normal application startup does not run these opt-in checks. Recorded speech used a synthetic WAV; the user's microphone was not recorded.

Run the merged build at `Builds/Integration-20261106/Windows/Lumen.exe` after starting the root `Start-Lumen-Hybrid.cmd` launcher. Older build folders are historical artifacts and do not contain the new frontend.

Raw evidence: `Builds/Integration-20261106/Player/hybrid-voice.json`, `front-page.png`, `world-atlas.png`, `source-preservation.json` and the build logs. Original project/build settings and the intended merged font were restored after Unity import/build. The temporary test backend was stopped; existing Ollama services were preserved.

Unity-generated metadata on the incoming branch contains trailing spaces on empty serialized fields; source-code/document diff checks pass. No frontend resources, scenes, packages or generated dependency files were rewritten to normalize formatting.

The merge is local. No branch was pushed and neither source remote branch was modified.

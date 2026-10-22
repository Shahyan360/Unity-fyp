# User-provided continuation brief — 22 October 2026

Act as the senior Unity engineer, technical artist, QA engineer, and release owner for *Lumen*. Continue the existing implementation and finish the project with verified results.

Complete the implementation, fix remaining defects, produce a fresh playable Windows package, update the documentation, and push the final code to GitHub. Do not stop at an audit or a plan.

## 1. Locate the project and recover its context

Locate the Lumen Unity project in the current workspace. Identify its repository root, current branch, installed Unity version, available tools, and existing implementation reports.

Do not assume any directory, drive letter, username, tool location, or runtime path from the previous computer.

Find and read, where available:

- The original Lumen implementation and visual-finalization brief.
- The professional project audit.
- Continuation notes and checkpoint reports.
- The issue-resolution matrix.
- The performance protocol.
- The shipped-asset ledger.
- Build reports, test results, and acceptance evidence.
- Applicable repository instructions.

Some reports may be stale. Reconcile them against actual source files, Git history, and evidence.

If supporting reports or art sources were not transferred, identify exactly what is missing. Continue independent work using available source and evidence. Do not invent results from unavailable reports.

Preserve the starting state before substantial changes. Use an isolated working copy or branch where appropriate. Do not overwrite unrelated work.

Use existing tools and dependencies where possible. Do not upgrade Unity or packages merely as routine cleanup. The previously used Unity version was *6000.6.2f1*; verify the version recorded by the project before opening or building it.

You may inspect applications and take screenshots when useful. Prefer direct project tools and APIs for reliable work. Do not automate login or expose credentials.

## 2. Understand the last recorded checkpoint

The last confirmed GitHub checkpoint was:

- Repository: Shahyan360/Unity-fyp
- Branch: codex/lumen-finalization-20261020
- Commit: f0c5701
- Commit message: Add live cancellation acceptance and available render counter telemetry

Verify the actual current repository state because newer work may exist.

At that checkpoint:

- The main implementation had been pushed successfully.
- New live-cancellation checks and render-counter telemetry had not yet been validated in a fresh Windows build.
- Character authoring corrections remained local and might not exist on GitHub.
- Temporary screenshots and a recovery scene were intentionally excluded.

Inspect whether the transferred project includes the unfinished art changes. A GitHub clone alone may not contain them.

Do not describe the source as fully release-validated until the current source has been built and tested on this system.

## 3. Preserve the working functionality

Lumen already supports five visitor experiences:

- Home
- Rome
- Classroom
- Museum
- Café

Museum is implemented as a Classroom variant, so the release build previously contained four scene entries.

Previously recorded results included:

- 86 backend tests passed.
- A Windows functional run passed 273 checks with no reported functional errors or detected text overflows.
- Functional coverage included all 28 supported classroom models, reversible inspection, notes, navigation, following, ordering, coaching, and multiple display sizes.
- Real-provider transport testing completed 36 questions across six guides.
- Follow-up provider tests checked Café versus Roman-market behavior.
- Live Editor testing verified actual Rome navigation, classroom actions, Café speakers, Museum, and Home.
- Backend restart recovery worked without restarting the player.
- Portable startup was tested outside the package directory under the same Windows user.

These are historical results from another computer. Preserve them as historical evidence and perform fresh validation on this machine.

The latest previously tested build was a release candidate, not the final application. The package staging directory also lagged behind that candidate. Rebuild and regenerate the package from the current source.

## 4. Finish the character corrections

Six authored characters exist:

- Kai
- Orion
- Professor Theo
- Aurelius
- Lucius
- Maya

The last complete turntable review found:

1. Kai’s trousers intersected the rear coat below the belt.
2. Aurelius’s trousers intersected part of the tunic.
3. Maya retained a white chef-hat fragment behind the hair.

Correction exports had finished generating on the previous computer, but visual review and Unity integration were still outstanding.

Locate any transferred authoring scripts, corrected exports, native reference data, masks, and turntable evidence.

If the corrections are absent, recreate the fixes from available source assets. Do not assume the GitHub version already includes them.

Complete the following:

- Review front, side, rear, and three-quarter views.
- Resolve intersections without creating oversized or distorted garments.
- Remove Maya’s white fragment without damaging hair, eyes, skin, or creating holes.
- Preserve earlier rejected evidence and clearly identify corrected evidence.
- Integrate the final assets while preserving required Unity GUIDs and prefab references.
- Preserve facial blendshapes, skeletons, weights, normals, UVs, and animations.
- Recheck idle, walking, pointing, gestures, speaking, and silent facial states.
- Recheck all six characters for regressions.
- Verify materials, shaders, components, and mesh buffers.

Make authoring scripts reproducible on this system. Replace undocumented absolute paths with configurable or project-relative paths.

The Maya correction previously depended on generated mask data outside the Git project. Include required small data in an appropriate tracked location or provide a reliable generation step.

Do not claim phoneme-aligned lip sync. The existing fallback uses spectral/envelope-driven jaw motion unless you implement and verify something more advanced.

## 5. Build and validate the complete application

Use the Unity version recorded by the project and its existing render-pipeline configuration.

Discover installed Unity, Blender, Python, and other tools on this machine. Do not assume they are installed or located where they were on the previous computer.

Check compilation, backend dependencies, and runtime configuration. Build a fresh Windows release candidate after integrating final assets and source changes.

Run the functional suite against that exact build. Verify:

- Home and every destination load and return correctly.
- Physical portal approach and interaction work for every destination.
- Grounding, collisions, navigation, and camera behavior work.
- Home following responds to player movement.
- Looking around while stationary does not make the guide orbit the visitor.
- Rome market and arena commands reach their intended destinations.
- Café ordering supports cancellation, restarting, waiting, and collection.
- The Café quiet-table/upstairs route works if advertised as reachable.
- Classroom requests select the correct model, especially Animal Anatomy versus Atom.
- Unsupported requests preserve the valid current model and explain limitations.
- Inspection restores the exact prior model state.
- Notes do not duplicate unnecessarily.
- Coaching modes complete expected steps and handle offline conditions.
- Museum labels honestly distinguish replicas and interpretations.
- Every intended interactive NPC has the correct identity, role, voice, and context.
- No runtime exceptions occur during acceptance.

Repeat layout checks at:

- 1024×768
- 1280×720
- 1920×1080
- A supported ultrawide resolution
- Normal and largest supported text sizes

Inspect Visit, Guide, coaching, chat, world-selection, and ordering panels.

Check clipping, overlap, scrolling, long input text, interaction hints, and horizontal fit. Record the resolution actually applied rather than merely the requested resolution.

## 6. Complete live conversation acceptance

Review the existing live-walk verification harness and run it against the final build.

The expanded harness was intended to cover approximately ten minutes across all five worlds with a bounded budget of 14 real-provider questions.

Required coverage:

- Remember the visitor name Robin and interest astronomy.
- Recall both after a backend restart.
- Recover without restarting the player.
- Start a real spoken response, cancel it, and confirm old audio stops.
- Confirm cancelled turns cannot publish late replies.
- Reconnect and recall the stored interest.
- Execute actual Rome market and arena movement.
- Test Lucius’s identity and historical-fiction framing.
- Exercise Atom and DNA requests and inspection.
- Test Museum replica explanations and inappropriate cross-world requests.
- Test Café identity, visitor-name recall, and conversation continuity.

Restart only backend processes owned by this test. Verify process ownership before stopping them. Preserve unrelated applications and services.

Use the harness’s dedicated restart window. Avoid manual interaction during automated scene transitions.

Also test:

- Rapid consecutive questions
- Cancellation during speech
- Guide or partner changes
- World changes during a pending response
- Backend unavailability
- Provider errors
- Reconnection
- Stale text, audio, images, or actions from an earlier turn or world

Use credentials supplied through an appropriate local configuration mechanism. Never commit or print them.

Distinguish synthetic text/audio tests from natural human microphone testing. Natural microphone quality, perceived voice quality, and device-removal behavior were not fully accepted previously.

Test those when feasible. If human participation is necessary, specify the exact remaining checks and provide straightforward instructions.

## 7. Complete image acceptance

A previous run exposed a failure when Image Retry was clicked before an image backend had been initialized. That source defect was fixed and passed a later functional check.

Recheck it in the final build and run the available bounded image-verification harness.

Verify:

- Explicit image requests work in intended contexts.
- Café images remain opt-in where designed.
- “Without images” requests are respected.
- Unsupported or irrelevant requests receive useful feedback.
- Retry works before and after successful requests.
- Backend, provider, and network failures are handled.
- New topics supersede older pending images.
- Scene and guide changes prevent stale images.
- Images match the active topic and scene variant.
- Evidence captures are nonblank and identify the tested build.
- Fixture tests and real-provider tests are reported separately.

Do not present fixture-generated results as real-provider success.

## 8. Validate performance on this computer

Discover and record this machine’s CPU, RAM, GPU, graphics API, display resolution, and quality settings.

Previous results came from different hardware and must not be treated as performance guarantees for this system.

Read the existing performance protocol. Keep these measurements separate:

- Unity frame interval
- CPU frame time
- GPU frame time
- Update/callback gap

Previously, Rome passed engine-frame, CPU, and GPU targets but missed a separate callback-gap p95 gate. Preserve that distinction.

A previous live run recorded one approximately 119 ms non-speaking, unfocused stall. Its cause remained unknown. A previously audited 190.9 ms speaking hitch was not reproduced.

Complete the following:

- Run the final build through the full live journey.
- Record the actual GPU and graphics API used.
- Avoid concurrent builds, renders, and unnecessary heavy workloads during measurements.
- Report per-world p95, p99, and significant stalls.
- Investigate repeatable spikes with evidence.
- Do not blame garbage collection without supporting data.
- Preserve failed runs and explain subsequent fixes.
- Use established performance targets; disclose any missing or unsuitable target before interpreting results.

Verify the latest telemetry:

- Exports available-counter inventory.
- Records supported render counters meaningfully.
- Represents unavailable counters as unavailable, not zero.
- Rejects allocation APIs that remain constant during a known allocation.
- Records GC collection deltas correctly.
- Flushes and closes output reliably.

If a development build is needed for additional diagnostics, label it clearly and keep its timings separate from release-build results.

## 9. Run the outstanding 30-cycle stability test

The long world-transition soak had not been completed at the last checkpoint.

Run 30 cycles using the existing harness where available:

- Rotate through Rome, Classroom, Café, and Museum.
- Return Home between visits.
- Allow defined settling periods.
- Record comparable idle memory and resource inventories.
- Track native and managed memory, meshes, materials, textures, audio, and persistent objects.
- Check exceptions, missing references, duplicate objects, audio leaks, and connection leaks.
- Distinguish cache warming from sustained growth.
- Investigate repeatable growth rather than forcing garbage collection and declaring success.

Record the exact build, machine, duration, cycle count, and result.

## 10. Finalize a portable Windows package

Generate the package from the final verified build.

Discover available runtime dependencies on this system and make required package paths relative or configurable. Do not carry forward absolute paths from the previous computer.

Review the launcher, packaging scripts, notices, and manifests.

Verify:

- Startup from outside the package directory.
- Startup from a directory containing spaces.
- Backend health checks.
- Port-conflict handling.
- Runtime and model discovery.
- Useful behavior when provider credentials are missing.
- Clear online and offline behavior.
- Graceful shutdown.
- Only task-owned services are stopped.
- Unrelated services remain running.
- No development-machine paths are required.
- No secrets, caches, recovery scenes, or unnecessary backups are packaged.

The earlier portable test used the same Windows user on the development machine. Running on this second system is important portability evidence, but document any dependencies installed manually or inherited from the environment.

Where practical, also test under a clean user or environment.

Provide clear setup and launch instructions, including:

- Required operating system and hardware
- Required dependencies
- Configuration and credentials
- Internet requirements
- Offline capabilities
- Troubleshooting
- Log locations discovered or created by the application

## 11. Close asset and runtime provenance gaps

Review the shipped-asset ledger and exact contents of the final package.

Some legacy vendor, font, Piper, and eSpeak-related source/license details still required closure.

For each shipped asset or runtime family, record:

- Source
- License
- Required attribution
- Modifications
- Required notices
- Any unresolved redistribution restriction

Do not invent permission or claim legal clearance without evidence.

A successful GitHub source push does not prove that the complete application is cleared for public redistribution. Clearly state any unresolved distribution requirement.

Do not purchase replacements or use paid generation without explicit authorization.

## 12. Update evidence and final documentation

Reconcile every original audit issue and every later defect against the final implementation.

For each issue, record:

- Problem
- Change
- Evidence
- Tested build and commit
- Result
- Remaining limitation

Use honest statuses:

- Fixed and verified
- Fixed, awaiting a named check
- Partially addressed
- Unresolved
- Requires external or human validation

Do not mark an issue complete merely because implementation code exists.

Create a before/after gallery where valid baseline captures are available. Some earlier captures were black; do not use them as visual evidence.

Deliver:

- Final playable Windows package
- Setup and launch instructions suitable for a different computer
- Final build identifier and source commit
- Backend and functional test summary
- Character, navigation, conversation, and image evidence
- Performance results with hardware and measurement definitions
- Stability-soak results
- Updated issue matrix
- Asset and runtime notices
- Known limitations
- Reproduction instructions for remaining defects

Keep the main report understandable for a nontechnical project owner. Place detailed diagnostics in supporting files.

## 13. Commit and push the final implementation

GitHub push is authorized.

Verify the configured repository and use the existing finalization branch unless the current repository state requires another appropriate feature branch.

Before pushing:

- Review the diff.
- Check for credentials and accidental generated output.
- Include required source, assets, metadata, authoring dependencies, and project documentation.
- Preserve Unity GUIDs.
- Exclude temporary screenshots, recovery scenes, caches, and secrets.
- Run checks appropriate to the changes.

Push meaningful checkpoints and a final verified checkpoint.

Do not force-push, merge into the main branch, or publish a public release without separate authorization.

## Working expectations

Continue autonomously through implementation, art corrections, testing, packaging, documentation, and the authorized GitHub push.

Do not repeatedly request permission already granted.

Do not stop after listing problems. Resolve them and verify the results.

Do not hide failures, overwrite evidence, suppress warnings for appearances, or claim untested behavior works.

Do not call Lumen fully finished until the final source, assets, build, package, and acceptance evidence agree.

If a missing file, credential, dependency, or human check prevents complete closure, finish all independent work and state the exact remaining requirement.

Start by locating the project, recovering the current state, and producing a short evidence-based list of remaining work. Then immediately execute it.

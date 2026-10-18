# Current frontend validation

Scope: expanded Unity frontend. Ollama/backend development excluded. This records evidence; it does not certify photoreal art quality.

## Final result at19:15

Frontend integration is **Ready with limitations** for local review. Original high-end art aspirations are not certified complete. Current Windows player journey passed52/52 checks and captured17 rendered views. Report: D:/LumenProject/Verification/Standalone-Final-2026-10-17/journey.json. Contact sheet and full-resolution PNGs were visually reviewed, with key views also inspected individually. All views have0 missing components and0 missing/error material shaders. Sampled navigators:9/9 in Rome/museum,3/3 in cafe,1/1 in science. Native log has no initial agent-registration failure after the deferred initialization repair. Its optional D3D12 debug-info-queue query warning did not prevent NVIDIA T500 rendering.

Current executable: D:/LumenProject/Builds/Windows-Frontend-2026-10-17/Lumen.exe. Updated build succeeded,615,773,560bytes,43.23seconds. The report counts1 error: Pipeline's5-second Editor-eval timeout during the synchronous build; build-messages.txt confirms this exact tooling origin.502 warnings comprise500 shader compiler warnings (predominantly Unity inference package, plusTMP), one pre-bake notice for3 retired Egyptian collision meshes, and one optional Pipeline-runtime-config notice. These are recorded, not suppressed. The first build succeeded with0 errors before the final fixes; its report is archived separately. Do not describe the final build as having a zero-error summary.

Short visible-player samples on this machine average~6.1-11.3ms for the world views, highest sampled frame17.15ms; total Unity-allocated memory peaks~607MB in the journey. These are brief traversal samples with the Editor also open, not an exhaustive profile or hardware guarantee. Hidden first-run images/timings are excluded because the window did not render.

Final fixes: retired the unused unmaterialed HomeScene BackgroundFloor, added dark painting-caption plaques, serialized only the three named scenario guide agents disabled and enabled each after a successful NavMesh sample. Scene backups and exact repair record live under D:/LumenProject/Archive/BeforeDeferredGuideNavigation. Scene saves also serialized existing empty ObjectMetadata fields. Player test preference changes restored; app exited; Editor stopped onHomeScene.87 old imported capture/archive files hash-verified onD before removing Assets/Logs.

| Requirement | Evidence | Status |
|---|---|---|
| Continue existing work | City/museum/hosts retained, no scene reset or repeated downloads | Passed |
| Human hosts | Six valid Humanoid prefabs,175 facial shapes each; distinct licensed Kai/Orion/Theo bases | Passed |
| Guide selection | Actual listener cycled six appearances in Editor and current player | Passed |
| Roman district | Twelve full-size shop-houses, two open temples, four stocked stalls, paved streets, eight olive planters, clothed residents | Integrated and visually inspected |
| Arena | Two animated combat performers and48 statically baked spectators; missing thigh geometry discovered and fixed; final short sleeves/hem rendered in player | Passed with static/repeated crowd-art limitation |
| Rome collision/navigation | Actual walking stopped at stall counter, climbed temple steps and entered cella; companion temple request PathComplete | Passed in Editor |
| Museum rooms | Central hall plus natural-history/painting/history wings; actual west doorway walking and three complete wing navigation paths | Passed in Editor |
| Exhibits | Two original skeletal studies, retained Smithsonian/history models, three Met paintings; upright Washington bust and readable raised labels | Visually inspected |
| Image quality | Met originals normalized up to2048px, preserved aspect; native1600x900 Unity view inspected | Passed in Editor |
| Facial animation | Removed optional jaw/eye retargeting that corrupted source axes;175 shapes retained, Maya jaw71.95/blink100 in speech observation | Passed in Editor |
| NPC speech ownership | Merchant2,250frames/jaw69.52/other-guide jaw0. Museum roaming visitor768 NavMesh-valid frames, speed1.382m/s, sampled speaking speed0, jaw71.53/blink99.89, other-guide jaw0; resumed walking3m | Passed in Editor with local Piper |
| Cafe | Correct Maya body, seven clothed background people, espresso/plants/pastries. Local order added/prepared/ready/collected | Passed in Editor; backend bypassed only in test |
| Opening | Four cards, isolated human preview, courtyard restores camera/layers; actual Rome/Science transitions | Passed in Editor |
| Cleanup | Four unused scenes archived before removal;87 capture files copied to D and hash-verified before removing imported Assets/Logs | Passed |
| Compilation | Unity6000.6.2f1 Windows64 Mono, D3D12/D3D11, all six host resources valid | Prebuild passed |
| Windows build | Updated current build succeeded;1 tooling timeout/502 warnings classified above | Passed with recorded tooling/package notices |
| Standalone journey | Real Rome card, all worlds, six guide appearances, resources, navigation, components/materials;17 rendered captures |52/52 passed |

Evidence: D:/LumenProject/Verification/FinalFrontend. EditorCaptureArchive contains failed iterations too; file names alone do not establish approval. Art sources, renders and canonical16-export index-count/hash manifest are in D:/LumenProject/Art. Latest Roman outfits include thighs, fitted leg skins, short linen sleeves and wider hem. Do not revert to calf-only costumes. D:/LumenProject/References holds pinned host receipts and authoritative Met public-domain records/normalized originals. PROJECT_PROGRESS.md is mirrored on D.

Material limits:

- Original stylized costumes adapt the user references; they are not exact anime/Behance/Blender Studio Victor assets. Faces use realistic Rocketbox humans; garment shapes/materials remain simpler than high-end character art.
- Speech animation follows audio amplitude and expressive morphs, not aligned phonemes. Numerical mouth-motion evidence does not assess voice prosody.
- Audience is static/shared and faces repeat. Only the two arena performers animate.
- Architecture/skeletons are labelled interpretive studies, not archaeological/anatomical scans.
- Local Piper is installed at D:/LumenTools/Piper. Other machines need it or LUMEN_PIPER_HOME; Windows speech is the fallback. Backend/Ollama not assessed.
- Editor timings are not standalone performance compliance. Player journey samples are short and specific to this machine; no cross-hardware budget asserted.
- Pre-existing account warning and Pipeline command timeouts are distinct from gameplay exceptions. Fresh prebuild Console had no current compile/gameplay errors/warnings.
- New asset provenance is documented; comprehensive legacy asset redistribution clearance remains unverified.

Build using Lumen/Validation/Build Windows on D or LumenWindowsBuild.Build in the connected Editor. Do not duplicate a running build. Inspect build-result.txt/build-messages.txt first.

Launch Lumen.exe normally for the app. The explicit opt-in `-lumen-verify-dir D:/LumenProject/Verification/Standalone-2026-10-17 -screen-fullscreen 0 -screen-width 1600 -screen-height 900` runs a bounded screenshot journey, restores its preference changes, writes journey.json and exits. No Ollama calls.

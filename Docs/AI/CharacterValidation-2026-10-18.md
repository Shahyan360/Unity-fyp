# Character upgrade validation — 18 October 2026

Status: ready with art limitations for the three-host character upgrade. This is not a claim that the entire original photoreal/world-class objective is finished. Backend/Ollama remains deferred and unchanged by this pass.

## Delivered models

| Host | New clothing | Clothing triangles | Facial shapes retained |
|---|---|---:|---:|
| Kai | Fitted forest tunic, red closures, saffron scarf, orange trousers, continuous boots, swept locks and cheek scar | 15,798 | 175 |
| Orion | Fitted dark coat, short mantle, teal edging/sash, top hat and eye-fitted glasses | 27,795 | 175 |
| Professor Theo | Fitted brown coat with native folds/pockets, ivory shirt inset, trousers, shoes and glasses | 25,863 | 175 |

Anatomy/weights derived from MIT Microsoft Rocketbox; original accessories/fabric maps. Exact80 native bones and bind poses, one shared clothing skinned renderer per host,1024px maximum compressed mipmapped fabric maps. Facial runtime binds12 compatible AK channels, with gentle host-specific resting expressions. Source FBXs/avatars preserved. Complete dressed prefabs under Assets/Prefabs/LumenAgents, lightweight runtime definitions in Resources/TailoredCharacters. Maya and Roman styles retain the previous licensed/classic implementations.

Blender API source Tools/Art/create_tailored_characters.py; Unity importer Assets/Editor/LumenCharacterTailoring.cs. Sources/renders/weighted JSON/GLB previews/receipts/license D:/LumenProject/Art/CharacterTailoring. Triangle admission reverses Blender CCW into Unity CW, preserves outward normals, uses inverse transpose for normal conversion. This fixed demonstrably black thin panels in live Unity. Custom coat tails have1.8cm radial walking clearance. Clothing bounds reuse native rig bounds expanded.45m; grounding uses the body, preventing clothing from changing foot calibration. Standalone dressed prefabs all validated missing scripts0 and clothing marker1.

## Execution evidence

Unity6000.6.2f1 URP, Windows x64 release Mono. Editor compiled and fresh Play inspected Kai/Orion/Theo (one fitted renderer,12 bound face channels). Actual Editor captures D:/LumenProject/Verification/CharacterUpgrade-2026-10-18. Runtime diagnostics were discarded on stopping Play. Scene assets were not edited in this pass.

Current build: D:/LumenProject/Builds/Windows-Characters-2026-10-18/Lumen.exe. Build summary Succeeded,673,852,728bytes,55.71seconds,502warnings and1tooling error. The error is Pipeline's5second eval timeout while the synchronous build successfully continued. Warnings are package shaders/TMP, retired mesh collision pre-bake and optional runtime Pipeline configuration. Do not describe the build summary as zero errors. Older verified frontend build remains preserved separately.

Final VISIBLE player journey D:/LumenProject/Verification/Characters-Final-2026-10-18/journey.json completed 00:00:45–00:01:55UTC on18October: **82/82checks passed**,26genuine1600x900screenshots, errors=[],invalidMaterials=[]. Covers opening Rome card, market/arena, museum wings/paintings, six actual guide-selector appearances, three fitted models in walk poses, gesture requests and actual local Piper speech, cafe/Maya default, science and return home. All sampled navigators valid (9/9Rome/museum,3/3cafe,1/1science), no missing components or invalid visible materials.

Actual local audio jaw maxima: Theo70.04,Kai71.39,Orion71.04. Speaking portraits show moving mouth and conversational hands. Walk screenshots inspected; Kai's earlier small trouser-through-tail patch is absent after clearance fix. Point action was requested; still captures alone do not establish a complete pointing gesture cycle. No backend request used for these speech checks. Test preferences restored and player exited; Editor stopped onHomeScene, compilingfalse.

First character run is preserved as FAILED evidence D:/LumenProject/Verification/Characters-Player-2026-10-18:76/82passed, six speech/audio jaw failures. Direct installed Piper diagnostic succeeded1.55s. Explicitly redirecting and draining the child process stdout (rather than inheriting a Windows GUI player's console handles), alongside stderr, fixed real player synthesis. Final run verifies playback and jaw movement on all3. No voice installation/download or Ollama modification.

Brief T500 samples average3.12–11.29ms per view, peak Unity allocated memory636,584,475bytes (~607MiB). These are short scene samples, not long-session/hardware certification. Player log reports optional stripped depth-of-field/Panini post effects and the existing D3D12 info-queue query notice; final captures render successfully. No new renderer/package configuration was introduced to suppress them.

## Remaining art limits and reliable next work

Models are reference-inspired realistic human adaptations with stylized accessories, not exact anime replicas or film-quality cloth/hair. Cloth follows skin weights without physical simulation; hair still benefits from a future dedicated sculpt. Facial motion is audio-amplitude-driven, not phoneme-aligned. Crowd diversity, fossil anatomy, higher fidelity environments, voice prosody and broader accessibility/input/long-session testing remain from the overall aspiration. Ollama remains user-deferred.

Do not repeat finished downloads/exports, overwrite original16-asset manifest, or treat failed first captures as current. Use final player screenshots/report above. Reproduce the three costumes through Build(style); a5s tool timeout can still leave the operation finishing, so check receipts before retrying. If source changes, stop Play first, refresh/compile, build and rerun the visible journey. All large sources/builds/evidence stay onD. Latest progress mirror D:/LumenProject/PROJECT_PROGRESS.md.
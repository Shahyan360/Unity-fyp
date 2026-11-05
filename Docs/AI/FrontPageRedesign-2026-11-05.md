# Front page redesign — implementation and evidence

> Runtime follow-up completed: the final Play Mode journey passes 31 checks with zero application Console errors; all 106 backend tests pass. Real destination previews have been captured and the atlas/menu were visually checked at 1024×768 and 1280×720. See the follow-up section below; the earlier source-only milestone is retained as history.

## Inspection and plan

Baseline: Unity 6000.6.2f1, URP 17.6.0, uGUI/TMP, Input System 1.20.0. No dedicated Unity MCP is exposed; Unity CLI is installed, but initially reports no connected Editor.

Existing edits at start: ScenarioTransitionService, WorldExperienceDirector, WorldVisitControls, TMP fallback asset; untracked LumenFrontPage.cs and recovery scene. Preserve all of these. No scene, prefab, backend or package replacement is planned.

HomeScene retains HomeManager/HomeUIManager, HomeAgent, Main Camera, HomeCanvas, EventSystem, light, platform and prototype controls. WorldExperienceDirector disables the prototype canvas and constructs LumenWalkableHubHUD. WalkableHomeHub constructs the garden, WorldExplorer/camera, Kai (Marcus fallback), and four portals; the three requested destinations plus the existing museum. AgentAnimator uses the existing humanoid controller, CharacterFacialAnimation supplies blinking/expression and gaze is already implemented. The current untracked front-page layer is incomplete: map previews are missing, chooser/map use the same view, camera framing is approximate, and menu state is forced every frame.

Reuse ScenarioTransitionService.RequestTravel for validated asynchronous scene navigation: cafe → Cafestore, classroom → Classroom, ancient_rome → HistoricalPlace. Retain museum and all doors. No durable complete-session save exists; Resume must explain this honestly. Existing visit drawer owns graphics quality, speech/UI cue volume, text size, reduced motion, movement layout, visual aids, connection and notebook. The mixer has Speech/UI/Ambience groups but no dedicated music group or exposed parameters. Do not invent a music setting.

Stages:
1. Extend the existing front-page layer with explicit screen state, saved/restored camera/guide/control state and cinematic lighting scoped to the menu.
2. Add separate environment cards and an illustrated atlas with three selectable regions, matching markers and descriptions. Load previews once; bake previews from actual authored scenes using editor tooling.
3. Reuse the settings drawer; add supported display, master-volume and mouse-sensitivity preferences there. Preserve all accessibility controls.
4. Prevent portal shortcuts while a menu is open; reduce repeated accessibility writes without changing AI, voice, backend or navigation ownership.
5. Compile with Unity where possible, run source/reference checks, exercise runtime UI/navigation, capture several aspect ratios, and report evidence and any blockers honestly.

The referenced colorful map image is absent from the provided attachment (text only). Atlas art is an original schematic interpretation of the described direction, not a map of the real environments. Existing URP ACES/color balance/SMAA and quality presets are reused; no global quality increase.

## Results

Initial source-only milestone: implementation compiled, but visual acceptance and preview capture were deferred when the user chose non-Play-Mode checks. The later request to run and fix the application authorized the runtime follow-up below.

### Implemented behavior

- Real-time front-page layer over the existing courtyard. It frames the existing human host on the right and presents seven menu actions on the left. No static background replaces the 3D world.
- The guide keeps its existing humanoid idle, blinking, facial expression and gaze systems. A short nod uses AgentAnimator; no automatic speech or additional AI backend is initialized by the new layer.
- Start Experience returns to the existing walkable courtyard. Camera position, orientation, FOV, guide orientation, movement and follower enabled states are preserved/restored when opening and closing the menu. Camera blending respects Reduced Motion. Escape returns from pages to the menu and can return to an already started courtyard visit. A short entry transition consumes Escape to avoid contradictory UI/control states.
- Choose Environment has three separate cards. World Map has three colored, illustrated regions with coastlines, sand borders, rivers, trees, mountains, town details, navigation routes and a compass. Each selectable marker updates the name, description, selected marker state and preview area; Enter Environment calls the existing validated route service. The geography is explicitly schematic, not a new navigable environment.
- Resume explains that complete saved-session restoration is unsupported. About contains the full project title and the tutor/guide/social-coach introduction; its body scrolls for enlarged text.
- Settings reparents the existing visit drawer temporarily, returning it to its original owner on close. It retains graphics presets, speech/UI cue levels, text tiers, reduced motion, movement layouts, visual aids, connection and notebook controls. A subpage adds master volume, pointer sensitivity, fullscreen and available display resolutions using the existing preference store. No music control was invented because no dedicated music channel was found.
- Exit uses Application.Quit in a player and ends Play Mode in the Editor.

### Files

Extended existing unfinished source:

- `Assets/Scripts/UI/LumenFrontPage.cs` (+ existing/new Unity metadata): screen state, menu, chooser, atlas integration, shared settings presentation, camera restoration and portrait lighting.
- `Assets/Scripts/Runtime/WorldVisitControls.cs`: retain existing public drawer integration; add supported display/audio/pointer subpage.

Modified:

- `Assets/Scripts/Runtime/WalkableHomeHub.cs`: portal input returns while an accessibility/menu overlay owns input; existing portal routing and geometry are unchanged.
- `Assets/Scripts/CafeWalkController.cs`: apply the saved pointer-sensitivity multiplier to existing look sensitivity.
- `Assets/Scripts/UI/LumenAccessibility.cs`: delegate front-page Escape handling; avoid repeated unchanged font/color assignments and repeated chat layout refreshes.
- `Docs/AI/UnityProjectContext.md`: current-machine/context correction above the historical record.

Created:

- `Assets/Scripts/UI/LumenAtlasTerrain.cs` + `.meta`: static illustrated atlas geometry, no per-frame mesh or texture generation.
- `Assets/Scripts/Config/LumenDisplayPreferences.cs` + `.meta`: apply existing-store display/master-audio preferences once at startup. Display overrides apply in standalone players, not the Editor.
- `Assets/Scripts/Runtime/LumenFrontPageProbe.cs` + `.meta`: opt-in, Editor-only review journey and actual runtime preview capture. Never installed by normal startup; restores the previous selected-scenario preference on disposal.
- `Assets/Editor/LumenFrontPageValidation.cs` + `.meta`: menu entry for that review.
- This report.

The pre-existing `WorldExperienceDirector` call to `LumenFrontPage.Install()` remains the installation point. The pre-existing ScenarioTransitionService changes, TMP fallback modifications and recovery scene were not replaced or discarded. No scenes, prefabs, packages, backend files, animation controllers or global quality assets were changed.

### Architecture and navigation

WorldExperienceDirector → existing WalkableHomeHub/host and HUD → LumenFrontPage.Install. The new scene-owned overlay waits for existing startup/controller placement, caches references and textures, builds screens once and delegates travel to ScenarioTransitionService.RequestTravel. It does not replace HomeManager or own AI/voice processing. It disables the legacy canvas/raycast interaction during front-page presentation, rather than destroying it. The existing museum remains available through the existing courtyard/portal flow.

| Destination | Scenario ID | Existing enabled scene |
|---|---|---|
| Café | cafe | Assets/Scenes/Cafestore.unity |
| Classroom | classroom | Assets/Scenes/Classroom.unity |
| Ancient Place | ancient_rome | Assets/Scenes/HistoricalPlace.unity |

### Graphics and responsiveness

The existing PC URP asset renders at scale 1, uses a 2048 main-light shadow map and four additional lights per object; its quality override is selected in QualitySettings. Existing ACES, color balance, bloom, vignette and antialiasing presets are retained. Two short-range, non-shadow-casting portrait lights operate only during menu presentation, alongside the existing garden directional shadow. No additional reflection probe, global shadow increase, material replacement or texture upscaling was introduced without rendered evidence.

Front-page UI uses a 1440×900 CanvasScaler with Expand, anchored page regions, consistent button padding, shared rounded-corner sprites, explicit hover/focus colors and 14–36 reference-point typography. Accessibility still owns final readable text sizing. The About body scrolls. Preview selection has a short tint transition, disabled for Reduced Motion. Actual layout at 16:9, 16:10, 4:3, ultrawide and enlarged-text tiers remains unverified.

### Performance changes and limits

Code inspection found that LumenAccessibility assigned font and button presentation repeatedly and refreshed chat layout every second even when display/text settings did not change. It now writes font sizes only when different, styles each discovered button once, and refreshes chat readability on resolution/text-tier changes. Dynamic controls are still discovered periodically. The atlas builds one mesh on UI invalidation, preview resources load once, screen selection uses cached references, and camera/selection animation coroutines finish instead of running permanent Update loops. No FPS, latency or allocation improvement is claimed without equivalent measurements.

### Validation evidence

| Check | Result |
|---|---|
| Unity script compilation of intermediate implementation | Passed: Tundra runtime and Editor build success in Logs/FrontPage/ValidationEditor.log. Existing UAC0005 warning in LumenAudioAuthoring remains. |
| Final runtime C# compilation | Passed using Unity's bundled .NET/Roslyn compiler and generated project source/reference lists. |
| Final Editor C# compilation | Passed against the freshly compiled runtime assembly and existing Unity references. |
| Player conditional C# compilation | Passed with UNITY_EDITOR/UNITY_EDITOR_64/UNITY_EDITOR_WIN removed. This is not a player build. |
| Routes, menu entries, host/controller assets and metadata | 18 source/reference checks passed; Logs/FrontPage/source-checks.txt. |
| Whitespace/diff check | Passed; existing line-ending notices only. |
| Startup, live character, click/keyboard journey, camera restoration and scene arrivals | Not run in Play Mode. |
| Screenshot/layout review and graphics/performance measurement | Not run. |
| Real destination preview images | Not generated; missing previews are explicitly labeled in the UI. |
| Existing doors, text/voice/AI/backend/NavMesh regression journey | Not run. Source systems retained; their runtime correctness has not been re-established. |
| Standalone build and Exit execution | Not run. |

Compilation used Unity's bundled compiler directly for the final revision because the machine-wide dotnet command has no SDK and generated-project MSBuild restore lacked its expected assets/config access. All compiler output assemblies and response files are confined to ignored Logs/FrontPage. The Editor was launched for validation, reached compilation, and then waited at Unity Editor Software Terms. Computer-use observation identified that dialog; access to inspect it timed out. The user elected non-Play-Mode checks, so no terms acceptance, Play Mode entry or UI actions were performed.

### Remaining work and setup

No Inspector wiring is required: the existing installer creates the layer and shared settings references. **Acceptance is not complete.** Once the user resolves Unity's startup dialog and chooses to run Play Mode, use `Lumen > Validation > Review Front Page and Three Destinations`. This opt-in journey checks the screens/control restoration, traverses all three existing routes, captures UI images under `Logs/FrontPage/Journey`, and writes actual environment camera images to `Assets/Resources/DestinationPreviews/{cafe,classroom,ancient_rome}.png`. Inspect these captures and preview imports visually; include their generated `.meta` files with the image assets. Reopen HomeScene/Play Mode after capture if necessary to reload preview resources.

Then test mouse/keyboard interaction, portal doors, backend text and voice, facial/locomotion animation, full-screen/resolution changes, all text tiers and several aspect ratios. The imported map reference remains unavailable, so matching its exact composition/colors requires that reference later. Font choice remains the existing TMP default. Music-specific mixing, durable session restoration and high-end new character/lighting assets were deliberately not fabricated. Full saved-session resume therefore remains an explanatory action rather than restored history.

## Runtime follow-up — run and correct the application

Confirmed and corrected:

1. The Unity Game view had a zero-height render target and emitted repeated `RenderTexture.Create failed` errors. Resizing/reopening that Editor view restored valid rendering. This was Editor layout state; no production rendering settings were increased.
2. `LumenFrontPage` used C# null-coalescing on a native `CanvasGroup` returned by GetComponent. Unity's Editor missing-component object was retained, causing MissingComponentException in CaptureVisit and leaving the legacy HUD visible. It now uses Unity's null comparison and explicitly creates the missing CanvasGroup. The previously failing menu journey passed after this fix.
3. `LumenAtlasTerrain` lacked CanvasRenderer. Runtime inspection found an active graphic with a valid rectangle but no renderer or mesh. Added RequireComponent(CanvasRenderer); the follow-up check confirms rendered geometry, and screenshots confirm visible coastlines, rivers, region details and markers.
4. The opt-in test's fixed four-second return-home wait could expire during scene initialization, before the front page's two-frame startup completed. The page was verified open afterward. The test now waits for HomeScene **and** LumenFrontPage.IsOpen, with the existing bounded deadline; it still fails if readiness is not reached.

Final evidence:

- `Logs/FrontPage/Journey/journey.txt`: **31 passed, 0 failed, 0 application Console errors**, COMPLETE. Covers startup/advancing frames, host/portal presence, Resume explanation, distinct chooser/atlas, actual atlas geometry and destination selection, About, shared settings, Escape, courtyard entry, movement/camera restoration, all three scene arrivals with AI clients/animators retained, and return-home readiness.
- `Assets/Resources/DestinationPreviews/{cafe,classroom,ancient_rome}.png` and `.meta`: actual 960×540 environment camera captures, imported and displayed in the chooser/atlas. No longer placeholders.
- `Logs/FrontPage/atlas-1024x768-verified.png` and `main-menu-verified.png`: visual inspection of the atlas at 4:3 and front page at 16:9. Used the Game view render target, because direct ScreenCapture outside the end-of-frame path can crop on the machine's scaled display.
- Backend suite: **106 passed in 11.40 seconds**. Command: backend `.venv/Scripts/python.exe -m pytest tests -q -p no:cacheprovider`, with basetemp under workspace Logs. Local `/health` reports the existing embodied-ai-local service healthy; a `/chat` request returned the curated Atom response with show_model action and Atom target.
- Initial failure evidence is retained under Logs/FrontPage, including the full Editor log, journey variants and console-history-before-clear.json. Old errors were cleared only after archiving evidence and completing the passing journey.

No authored scene, prefab, backend implementation or package was replaced. The TMP fallback asset also received Unity-generated glyph/cache updates while rendering; its pre-existing user edits were retained. Temporary screenshots were moved out of Assets/Logs into ignored Logs. The only new production assets from runtime validation are the three destination preview textures and their metadata.

The application is left in Play Mode at the front page. Remaining validation limits: physical walking through each door trigger, actual microphone/speech-recognition input, all settings permutations/text tiers, ultrawide/portrait layouts, measured performance comparisons and a fresh standalone build have not been verified in this follow-up. Direct scene travel and portal presence passed; this does not substitute for a physical trigger/input test. No durable full-session save was added.

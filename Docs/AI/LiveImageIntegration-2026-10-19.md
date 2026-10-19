# Live educational images - 19 October 2026

## Current user-approved policy

| Scenario | Image behavior |
| --- | --- |
| Home, Ancient Rome, Classroom, Museum | Automatically attempt a relevant image alongside each visitor question, including follow-ups. |
| Cafe, both helper and addressed guests | Voice/text by default. Retrieve an image only on an explicit picture, diagram or visual-explanation request. Ordinary factual questions and social coaching do not request images. A no-more-pictures request clears the image. |

Gemini chooses a short semantic topic matching the explanation. Electrical transformers and AI transformer architecture are separate topics; ambiguous meanings should be clarified. Unity also enforces cafe request intent and blocks automatic fallback there. Automatic greetings do not count as visitor image requests. Every applicable question attempts retrieval; suitable images are not guaranteed for every topic.

The LOOK CLOSER panel displays the image, creator, license and credit. + enlarges it, X dismisses it, PICTURE reopens it; SOURCE and LICENSE open credited pages; RETRY repeats a search. Compact docking uses the active speaker head projection, with hysteresis and a0.5second check while visible, to keep moving guides clear. Expanded viewing intentionally occupies the center. Images supplement interactive classroom models; they are not represented as new3Dassets.

New questions clear old images. A revised request, world switch or partner switch cancels the client request and rejects late results by revision. Old topic/credit text is cleared too. If the model omits the tool, a best-effort topic fallback runs outside the cafe and on explicit cafe visual requests. Short NEW topics such as DNA do not reuse the previous Eiffel Tower image. Image lookup runs independently of speech; tool confirmation means searching started, not that an image has already appeared.

## Backend and storage

`embodied-ai-backend/live_images.py`: POST /images/search with JSON query(max160characters), GET /images/file/<sha256>. Uses the official en.wikipedia.org MediaWiki search/pageimages API, followed by shared Commons imageinfo metadata. Commons DNS did not resolve on this connection; Wikipedia's supported shared-media API works. No Google Images scraping and no additional image API key. The current Gemini credential remains private in the backend.

Only eligible CC BY, CC BY-SA, CC0 or public-domain metadata is admitted. Unknown/fair-use/noncommercial licenses are excluded. Model output supplies a semantic topic, never a fetch URL. Downloads accept API-selected HTTPS upload.wikimedia.org/thumb.wikimedia.org Commons media only, reject redirects, and enforce6MiB/8millionpixel limits. Pillow12.3.0 decodes/resizes to at most1280x1000 and converts to JPEG on white; the UI credits this modification. Text metadata is sanitized and displayed without rich-text interpretation. Unity accepts only a fixed local /images/file/<digest> path.

Default cache D:/LumenProject/Cache/LiveImages, configurable through LUMEN_IMAGE_CACHE. Successful files expire after7days; pruning retains at most256items/256MiB. Three simultaneous uncached searches; bounded network timeouts, explicit busy/offline/no-result messages. Cached files can still load when the external provider is unavailable. New failed topics never retain an earlier picture as their answer.

Provider coverage and semantic relevance have limits. No broad commercial image search or guaranteed safe-search classifier is included. Rare subjects may need a more specific query. Wikimedia metadata is the licensing source; source links provide full context and attribution. This is retrieval, not AI image generation.

Primary API references: https://www.mediawiki.org/wiki/API:Imageinfo ; https://www.mediawiki.org/wiki/API:Pageimages ; https://commons.wikimedia.org/wiki/Commons:API/MediaWiki .

## Validation

Baseline d49f439, branch codex/lumen-gemini-interaction-checkpoint, Unity6000.6.2f1 Windows64 Mono. Existing TMP fallback atlas was dirty before work and preserved unstaged. Prior Interaction/Character/Frontend builds preserved.

- Backend69tests passed after cafe policy change: request/origin/host/license validation, metadata sanitizing, cache hit/expiry, decode/alpha/oversize limits, provider failures, all-scenario prompts and cafe helper/guest opt-in policy.
- Real provider searches: electrical transformer, AI transformer architecture, Eiffel Tower, DNA, photosynthesis, Colosseum, Triceratops, active listening.
- Editor-First42/43: retained failed evidence. Rapid typed follow-up callback was incorrectly cleared by a delayed interruption; corrected callback lifetime through audio/tool output. Original panel also covered guides.
- Editor-Second45/45: real Gemini explanation/image changes, corrected transformer meaning, synthetic16k spoken question, DNA3Dmodel+image, all worlds, failed/stale/reset cases;11captures. This preceded cafe opt-in.
- Player-Final46/46: original automatic-image policy in executable; confirmed source topic explanations and fallback. FirstAudioSeconds contains a0.032second late-packet sample, so it is not reliable evidence of that new reply's first audio latency. Do not advertise this as an instant response.
- Player-ConversationRegression56/56: native synthetic barge-in, automatic market/arena walking, classroom displays, addressed NPC identity/voices, selected-speaker jaw motion/other jaws silent, cafe companion role and transitions. This preceded the final cafe/layout revision; common audio/navigation code remained unchanged afterward.
- Player-Adaptive51/51: updated cafe no-auto / explicit-image / no-more-images cases, plus the image journey. Museum guide clear. Rome screenshot revealed the guide moves left after initial docking; added ongoing0.5second compact-panel tracking with hysteresis.
- Player-Release51/51 PASS, errors=[],13actual1600x900captures on the final executable. Electrical/AI correction, synthetic spoken transformer image, DNA3D+image, photosynthesis, Rome, museum, cafe ordinary/no-image + explicitly requested image + no-more-pictures, honest failures, superseded download, short-new-topic fallback, scene reset. Screenshots inspected: Rome and museum guides now clear beside right-docked images; cafe ordinary conversation has no image, explicit request displays the credited image. No gameplay exceptions/failed-agent-creation pattern in the player log. Receipt: D:/LumenProject/Verification/LiveImages-2026-10-19/Player-Release/journey.json.

Final build succeeded0errors/1warning,673939925bytes/37.084seconds. D:/LumenProject/Builds/Windows-LiveImages-2026-10-19/Lumen.exe. Warning: optional developer Pipeline runtime configuration absent; normal game code works. Initial direct build included a Pipeline control-tool5second timeout in its error count despite a Succeeded result; retained as build-first*.txt. Rebuilt through a delayed Editor callback, final0errors. Preloaded asset settings are restored via PlayerSettings plus SaveAssets. Previous player baseline and current image player both have6DOF/Panini stripped-shader warning lines; no new gameplay exception was found in the completed runs. Human microphone/noisy-room/acoustic echo remains unverified by these synthetic trials.

## Run / resume

Run embodied-ai-backend/app.py, then the executable. The existing launcher uses D:/LumenTools/VoiceRuntime/Scripts/python.exe; requirements-voice.txt includes the base dependencies including Pillow. No image credential is needed. Backend current parentPID1132 (verify exact app.py process before restart); logs backend-cafe-policy*.log under the verification root.

LumenImageVerification is opt-in only: explicit StartRun(D-directory) in Editor, or -lumen-images-dir D-directory in a player. It disables/restores physical microphone preference and uses a local synthetic16k fixture on D. Never use test flags during ordinary play. Build method LumenWindowsBuild.BuildLiveImages preserves older builds and restores preloaded assets. For long operations use a delayed Editor callback so the control response returns before build work starts. Always inspect build-result.txt rather than retrying a timed-out build blindly. PROJECT_PROGRESS.md is mirrored to D:/LumenProject/PROJECT_PROGRESS.md.

Final state: Editor stopped at Home, no player/test/build process running, original microphone preference restored1(on). Backend remains running. No Ollama reinstall/download. Saved on codex/lumen-gemini-interaction-checkpoint; large binaries/cache/screenshots stay on D. Existing TMP fallback atlas edit excluded from this feature checkpoint.

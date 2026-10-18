# Approved environment implementation plan

Approved by the user on 2026-10-16. Read PROJECT_PROGRESS.md for current execution state.

## Non-negotiable direction

- Ancient Rome is a walkable CITY: full-sized buildings, streets, forum, markets and people. Never replace the main experience with a gallery of miniatures.
- Museum is an architectural place with entrance courtyard, arrival hall, connected natural-history and historical-collections sections, and substantial exhibits. Not an identical-pedestal grid.
- Reference images guide real buildings and exhibit staging. They are not ready-to-import geometry and do not mandate massive maps.
- Reuse suitable existing Roman buildings. Build missing/adapted assets with Blender Python when suitable licensed online assets are unavailable or local modelling is practical.
- Frontend only. Do not download, repair, replace, or configure Ollama. Backend, if needed later, runs existing app.py.
- D:/LumenProject is the main working folder. Import only necessary runtime exports into the existing Unity project on C. No paid purchases without permission.

## Stage 1: preserve and audit

Audit scene geometry, navigation, existing hosts, scripts, downloaded sources and licences. Back up replaced work. Determine reuse/repair/replacement per asset. Establish a compact layout and clear player routes before expanding content.

Actual reusable Roman structures: amphitheatre around (0,0,0), forum at (0,0,-33), temples at (±25,0,14), shops near (±25,0,-19), market buildings near (±31,0,1) and (±17,0,22), river at z=36. Existing site is approximately 78 m across; retain its footprint rather than expanding it. Audit receipt on D.

## Stage 2: representative Roman street/forum/temple

- Restore the authored HistoricalPlace scene as the active city.
- Improve amphitheatre openings, temple proportions, pediments, columns, capitals, cornices and roof forms at full scale.
- Add modular plaster/stone shop fronts, terracotta roofs, doors, shutters and recessed openings where useful.
- Compose arrival street, forum and temple views. Avoid isolated buildings surrounded by empty flat ground.
- Establish paving, kerbs, thresholds and restrained street details.
- Render Blender assets and inspect a representative integrated Unity section at player eye height before extending the same style.

## Stage 3: market and population

- Varied timber stalls, cloth awnings, counters, pottery, amphorae, baskets, crates, sacks and produce.
- Merchants behind counters, browsing customers, walking citizens, small conversation groups and selected seated figures.
- Initially approximately 8–12 background people; adapt to measured performance and asset quality.
- Suitable Roman clothing and varied appearances. Current modern humanoid visitors are interim placeholders, not finished residents.
- Reuse animation/navigation where possible; verify collision, stall access and pedestrian clearance.
- Marcus remains the main conversational guide. Fully conversational behaviour for every background person is outside this initial environment pass.
- Prioritise exteriors and only selected accessible interiors. No sprawling city or long empty roads.

## Stage 4: museum architecture

- Revise useful existing courtyard elements towards the user reference: classical facade, short stone path, layered planting, pools, sculptural features, lamps and benches.
- Build a recognisable arrival hall with identity/orientation signage, architecture, seating and a clear route to both exhibit sections.
- Keep guide positions welcoming and clear of circulation.

## Stage 5: natural history and historical collections

- One substantial dinosaur skeleton focal exhibit. Search for a suitable licensed model first; otherwise build an explicitly interpretive skeletal reconstruction in Blender, using anatomical references as well as mood images. Do not present an improvised model as scientifically exact.
- Low display base, rock/excavation staging, safe route around the exhibit, restrained barrier, supporting fossils/cabinets and readable panels.
- Retain useful existing museum specimens; correct orientation, scale and weak replicas where feasible.
- Historical collections grouped by subject using varied cabinets, niches, wall relief/map panels and display bases.
- Current fossil cabinet and entrance are reusable components, not the completed museum.

## Stage 6: finishing and verification

- Consistent limestone/plaster/terracotta/timber/cloth in Rome; controlled stone/timber/bronze in museum.
- UVs, surface textures, restrained wear, suitable daylight/shade or exhibit lighting, subtle ambience.
- Keep existing conversation, movement, guide and world-navigation systems. Update positions, interaction metadata, labels and controls for the real spaces.
- Inspect Blender renders and actual Unity screenshots; walk routes, check scale, materials, collisions, navigation and scene transitions. Fix defects before adding more content.
- Bound texture sizes, repeated materials, mesh complexity, character animation and lighting. Use measured performance; do not claim certification from an Editor sample.
- Fresh standalone build and smoke test after environment checks. Record any unverified controls, voice/privacy limitations and licence gaps.

## Cleanup and handoff

Archive before deleting referenced/replaced content. Check dependencies. Keep temporary captures and large sources out of Assets. Update PROJECT_PROGRESS.md after each significant stage with exact paths, implemented work, evidence, current issues and next commands. Preserve dirty user work and the installed Unity version.

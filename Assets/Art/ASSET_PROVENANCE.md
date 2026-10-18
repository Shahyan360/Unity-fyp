# Lumen added asset provenance

## Roman city street collection — 2026-10-16 approved city direction

RomanMarketStall is original full-sized geometry authored with Blender Python in `Tools/Art/create_roman_street.py`, using shared local authoring helpers. No external geometry/textures or paid provider was used. Source, GLB, preview and topology receipt are under D:/LumenProject/Art. 23,252 triangles, full-size canopy approximately 3.6 m wide and 2.95 m high, plus clay jars at its sides. It is an interpretive market reconstruction, not an excavated historical scan. Unity builder combines meshes by material; explicit counter/post colliders leave space beneath the canopy. The existing Roman city buildings are retained; their previous provenance still applies.

Current eight background humanoids reuse the pre-existing LumenPeople resources as interim visitors. They are not newly sourced Roman-dress characters; population art remains unfinished.

RomanArcadeRestoration is original Blender geometry authored by `Tools/Art/create_roman_arcades.py`, matched to the existing amphitheatre's three elliptical tiers (40 bays per tier). 12,960 triangles, combined into three material groups. It retrofits the real city building; it is not a tabletop model. Original piers, cornices, arena, collision and navigation are retained. No new third-party source or paid provider was used. Editable source, GLB, render and receipt are on D:/LumenProject/Art.

## Human hosts

Selected source assets from Microsoft Rocketbox, repository commit `0943055db6ec570bcef9f2c8b41c9e5467c808f9`:
https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9

- Ada: Business_Female_02 facial FBX and associated f015 textures.
- Maya: Chef_Female_01 facial FBX and associated f110 textures.
- Marcus: Male_Adult_14 facial FBX and associated m012 textures.

Copyright (c) 2020 Microsoft. MIT license retained in `Rocketbox/LICENSE.md`. Fictional character names and dialogue are project additions; these models do not portray the personas as real people. Imani currently shares Ada's model.

Original downloads are preserved under D:/CodexAssets/Rocketbox. `Tools/Art/prepare_characters.py` converts selected texture files to PNG at a 2048 maximum dimension and renders inspection previews. Original FBX rigs/blendshapes are preserved. Maya's source material `f110_A_head` maps to `f110_head_color_a.png`. `LumenCharacterAssetBuilder` creates Unity URP materials, humanoid avatars and resource prefabs.

## Blender collection

EspressoStation, DiscoveryOrrery and RomanStudy are original geometry authored by `Tools/Art/create_lumen_collection.py` using Blender Python. No paid provider or external geometry was used. Editable .blend files, GLB/FBX exports and inspected preview images are under D:/LumenArtifacts/Art. Small runtime GLBs are imported in this project and combined by material using `LumenPropAssetBuilder`.

- EspressoStation: 26,612 triangles, six combined material groups.
- DiscoveryOrrery: 40,184 triangles, 12 combined material groups; schematic modern teaching model with compressed planet sizes/distances.
- RomanStudy: 15,276 triangles, four combined material groups; interpretive architectural study, not a specific excavated monument.

These receipts apply only to the added assets. They do not audit pre-existing project assets, Smithsonian downloads, or legacy Asset Store content.

## Compact museum collection — 2026-10-16

Original meshes authored with Blender 4.5 Python in `Tools/Art/create_compact_museum.py`, inspired by the user's images and selected ideas in their museum brief. No geometry, textures, or paid assets were downloaded for this collection. The references are art direction, not a source of imported geometry.

- MuseumCourtyard: 50,924 triangles; level 15 × 11 m garden, classical facade, four columns, urns, pools, banners, hedges, cypress and lamps. The facade is 21 m wide to meet the existing gallery shell. Separate Unity colliders preserve the doorway and walkway.
- FossilCabinet: 13,968 triangles; original schematic ammonite and fern impressions on shale, not scans or scientifically exact fossil specimens.
- ForumTable: 46,796 triangles; 3.94 × 2.94 m tabletop exhibit with arcaded amphitheatre, two temples and houses. Interpretive architectural composition, not a geographical reconstruction of Rome.
- RomanArtifactCase: 6,468 triangles; original amphora and schematic coin replicas in a small walnut cabinet, not scans of excavated artifacts.

Editable `.blend`, GLBs, rendered previews and topology receipt: `D:/LumenProject/Art`. Imported runtime exports remain in `Assets/Art/LumenCollection`; `LumenPropAssetBuilder.BuildCompact()` combines hundreds of authoring objects into a small number of mesh/material groups for Unity. These are project-original assets, with no third-party attribution obligation arising from their geometry. Pre-existing scanned museum specimens retain their original provenance requirements.

## Neural speech

Optional local Piper runtime: https://github.com/rhasspy/piper/releases/tag/2023.11.14-2
Voice model: https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_GB/vctk/medium
Model card: https://huggingface.co/rhasspy/piper-voices/blob/main/en/en_GB/vctk/medium/MODEL_CARD

The upstream model card identifies the VCTK dataset as CC BY 4.0 and states the model was fine-tuned from the US English lessac voice. Preserve upstream runtime notices, model card and dataset attribution when packaging. Training-source terms must be reviewed before a public/commercial release; this document does not claim a complete redistribution audit.

Runtime and model are installed on D:/LumenTools/Piper, outside the repository. `LUMEN_PIPER_HOME` may select another installation; Windows installed speech voices are used when the optional files are absent. Current VCTK speaker IDs: Ada 107 (p225), Maya 85 (p229), Marcus/Lumen 95 (p226), Imani 90 (p228). These are synthetic voice choices, not voice cloning of the user's identity.

# Distribution evidence — 22 October 2026

Status: private acceptance only; public redistribution closure remains incomplete.
The earlier complete shipped-asset ledger was not transferred. A packed-asset
report identifies residency, not permission. Nothing below invents missing rights.

| Family | Available evidence | Modification / notice | Remaining requirement |
| --- | --- | --- | --- |
| Microsoft Rocketbox humans | `Assets/Art/Rocketbox/LICENSE.md`, MIT, Microsoft 2020; native sources and textures present | Retargeted/fitted garments and body masks; retain complete MIT text | Previous pinned download/source receipt is absent |
| Original Lumen garment/environment code | Tracked `Tools/Art` and imported authored assets; descriptions in `FrontendAssetProvenance.md` | Local geometry, fabric and fitted garment changes | Prior `.blend` files, 16-asset manifest and references not transferred |
| UnityTechnologies assets | `Assets/UnityTechnologies/license.txt` identifies Unity Companion License | Preserve notice and engine-use restrictions | Resolve exact shipped families from current packed-asset residency |
| Met paintings | Historical object IDs/URLs in `FrontendAssetProvenance.md`; runtime credits | Prior report records public-domain object flags | Original authoritative API/download receipts are absent; re-establish them before distribution |
| Legacy imported buildings, models and textures | Existing project assets | Do not remove attribution or substitute assumptions | Original vendor/source/license mapping remains unresolved |
| Fonts / TextMesh Pro | Imported font assets and package dependencies | Preserve included notices | Exact Liberation/vendor-font source and final shipped font closure remain unresolved |
| CPython | Active installed interpreter includes `LICENSE.txt`; packager copies it | Unmodified interpreter; excludes tests/caches and original site-packages | Review final inventory and all bundled runtime notices |
| Python dependencies | `RUNTIME_INVENTORY.json` from installed distribution metadata and copied dist-info notices | Google pure Python modules bundled as a zip; no source changes | Metadata alone is not legal clearance; inspect included notices against exact distribution |
| Piper / eSpeak / voice models | No installed model/runtime was found here; omitted by default | Optional `--piper` requires explicit directory | Source, version, voice model card and applicable redistribution obligations must be supplied before including |
| Local Whisper model | Not installed here; omitted by default | Optional `--stt-model` requires existing `model.bin` | Include model source, license and card if supplied |
| Live educational images | Backend accepts eligible Wikimedia attribution metadata and displays credits | Runtime cache; never copied by source allowlist | Per-image attribution and provider metadata remain conditional; no blanket licensing assertion |

Final packaging must copy applicable notices beside the player and retain
`RUNTIME_INVENTORY.json`, build GUID, source identity and `PACKAGE_SHA256.txt`.
Do not include credentials, user conversations, caches, rejected captures or
recovery scenes in the portable package.

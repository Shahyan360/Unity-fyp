# Distribution evidence — 22 October 2026

Status: private acceptance only; public redistribution closure remains incomplete.
The earlier complete shipped-asset ledger was not transferred. A packed-asset
report identifies residency, not permission. Nothing below invents missing rights.

`ShippedAssetFamilies-2026-10-22.csv` groups the detailed report from supporting
build `750b464c7b2d4371b8131217c95c7c65`. The corrected cached build's
`asset-build-residency.tsv` is header-only, so the earlier inventory is not
presented as a complete exact-build ledger. Notable unresolved legacy families
include acquisition records for `Assets/AssetProviderForAll/skyboxes` and
`Assets/Cute_Magic_Stylized_LowPoly_Free_01`. Their source IDs and current stated
licenses have now been recovered in `AssetStoreSourceEvidence-2026-10-22.md`.
A name containing Free is not license evidence. The final package checksum inventory identifies physical
files, not every embedded source asset or its rights.

| Family | Available evidence | Modification / notice | Remaining requirement |
| --- | --- | --- | --- |
| Microsoft Rocketbox humans | `Assets/Art/Rocketbox/LICENSE.md`, MIT, Microsoft 2020; native sources and textures present | Retargeted/fitted garments and body masks; retain complete MIT text | Previous pinned download/source receipt is absent |
| Original Lumen garment/environment code | Tracked `Tools/Art` and imported authored assets; descriptions in `FrontendAssetProvenance.md` | Local geometry, fabric and fitted garment changes | Prior `.blend` files, 16-asset manifest and references not transferred |
| UnityTechnologies assets | `Assets/UnityTechnologies/license.txt` identifies Unity Companion License | Preserve notice and engine-use restrictions | Resolve exact shipped families from current packed-asset residency |
| Met paintings | Current official collection API identifies all three bundled object IDs as public domain; URLs match the tracked painting manifest; local hashes in `Met-Provenance-2026-10-22.json` | Retain painting credits and source links | Original download chain is absent; current API verification does not recreate that receipt |
| Quaternius animations and Standard characters | Local `Animations/License.txt` and `BaseCharacters/License_Standard.txt`: CC0 1.0, Quaternius | Both complete notices copied into package | Resolve actual packed residency; do not extend Standard evidence to paid collections |
| Legacy imported buildings, models and textures | Existing project assets | Do not remove attribution or substitute assumptions | Original vendor/source/license mapping remains unresolved |
| Cute Magic / space skyboxes | Local AssetOrigin IDs 379394 / 282836 and current matching official pages; both list Standard Unity Asset Store EULA | Source links and recovered IDs retained in package notice; existing usage unchanged | Original acquisition/entitlement records and historical terms remain unverified; other legacy families still need mapping |
| Fonts / TextMesh Pro | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`: SIL OFL 1.1, Google 2010 and Red Hat 2012 | Complete notice copied; preserve reserved-name conditions | Final shipped font inventory and any additional vendor-font mapping remain unresolved |
| CPython | Active installed interpreter includes `LICENSE.txt`; packager copies it | Unmodified interpreter; excludes tests/caches and original site-packages | Review final inventory and all bundled runtime notices |
| Python dependencies | `RUNTIME_INVENTORY.json` from installed distribution metadata and copied dist-info notices | Google pure Python modules bundled as a zip; no source changes | Metadata alone is not legal clearance; inspect included notices against exact distribution |
| Piper / eSpeak / voice models | No installed model/runtime was found here; omitted by default | Optional `--piper` requires explicit directory | Source, version, voice model card and applicable redistribution obligations must be supplied before including |
| Local Whisper model | Not installed here; omitted by default | Optional `--stt-model` requires existing `model.bin` | Include model source, license and card if supplied |
| Live educational images | Backend accepts eligible Wikimedia attribution metadata and displays credits | Runtime cache; never copied by source allowlist | Per-image attribution and provider metadata remain conditional; no blanket licensing assertion |

Final packaging must copy applicable notices beside the player and retain
`RUNTIME_INVENTORY.json`, build GUID, source identity and `PACKAGE_SHA256.txt`.
Do not include credentials, user conversations, caches, rejected captures or
recovery scenes in the portable package.

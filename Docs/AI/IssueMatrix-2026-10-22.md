# Current acceptance matrix — 22 October 2026

The original audit matrix was not transferred, so these are request-derived
acceptance rows rather than invented original audit identifiers. Current starting
source is f0c5701 plus the explicitly documented continuation changes. Exact
player build GUIDs and run results belong in the new acceptance receipts.

| Problem / requirement | Change or current evidence | Status / remaining check |
| --- | --- | --- |
| Kai rear trousers intersect coat | 796 hidden trouser vertices tucked; baseline/final four-angle previews retained | Fixed and verified in sampled final-player walking/gesture/speech views (269 checks) |
| Aurelius trousers intersect tunic | 857 hidden trouser vertices tucked; baseline/final views retained | Fixed and verified in sampled final-player walking/gesture/speech views (269 checks) |
| Maya white cap fragment | Removed 26 cap UV-island triangles; extended existing crown after seam was observed | Fixed and verified in sampled final-player walking/gesture/speech views (269 checks) |
| Six-character rig/material regressions | All body inventories: 80 bones, 175 shapes, zero invalid materials | Fixed and verified for sampled poses: 269 checks, 70 final-player views; original rig/morph buffers retained |
| Authoring paths and missing Maya mask | Project-relative/configurable output; tracked UV mask and guarded recipe | Fixed, awaiting Blender regeneration on a machine with Blender |
| Stale Editor/player script layouts | First candidate failed with explicit layout mismatch; fresh Editor recompile and stale-source guard added | Fixed and verified: final player built and new header/capture checks ran; retained build diagnostics remain documented |
| Python backend health | 89 tests passed; earlier pip check clean | Fixed and verified for tested backend scope |
| Gemini credential/model access | Authentication accepted; real Live audio received; 14-question player walkthrough completed | Verified with the configured key and model on this machine |
| Home/world portals, following and stationary looking | Existing functional harness retained | Fixed and verified in final-player functional run (276 checks, zero errors) |
| Rome market/arena actual arrival | Existing functional/live harness checks movement outcomes | Fixed and verified in final-player functional run (276 checks) |
| Café ordering/cancellation/restart/collection and advertised routes | Existing ordering and navigation code retained | Fixed and verified in 276-check run and reviewed final UI captures |
| Classroom all models, Anatomy vs Atom, unsupported requests and inspection | Existing functional harness retained | Fixed and verified in final-player functional run (276 checks) |
| Notes, coaching and offline behavior | Existing functional harness retained | Fixed and verified in final-player functional run (276 checks) |
| Responsive panels at four display sizes and largest text | Existing checks record actual Screen dimensions and overflow | Fixed and verified: applied sizes checked, standard/largest text; five contact sheets reviewed; zero detected overflows |
| Live memory, restart, cancellation and continuity | Real-provider walkthrough completed 14 questions over 616.58 seconds, including owned-backend restart | Verified for the exercised questions, recall and recovery paths |
| Rapid/partner/world changes, stale outputs and provider failure recovery | Existing conversation/image harnesses and lifecycle implementation retained | 69 final conversation checks passed after backend corrections; broader unexercised failure modes remain limited to recorded coverage |
| Image retry before connection | Previously fixed source retained; functional harness checks it | 276-check final functional run passed; real image cases recorded separately |
| Images: relevance, opt-in Café, negative intent and stale topics | Existing bounded real-image harness retained | 73 final portable-player checks passed; DNA relevance regression fixed and final image captures reviewed; source/license metadata remains conditional |
| Performance metric conflation | Analyzer reports engine interval, CPU, GPU and callback gap separately | Measurements complete; numerical targets/protocol absent; no performance pass |
| Unavailable render counters / shutdown flushing | Missing samples become -1; close is idempotent and called on quit; native SRP/triangle/shadow counters recorded | Verified telemetry and shutdown flush; 1.2-second Museum tail event remains unresolved |
| Thirty-cycle memory/resource stability | Removed forced collection/unload; added persistent/audio/controller inventories | 30 offline cycles completed, zero errors; post-warmup native counts stable; allocated -0.08 MiB/private +1.04 MiB; managed +1.39 MiB; online connection-leak scope remains unverified |
| Portable packaging uses old machine paths | Explicit build/output paths, optional model/runtime paths and copied Python closure | 276 actual portable functional checks, 69 real conversation checks and 73 image checks passed from an external folder with spaces; clean-user/other-device validation remains |
| Deep Google SDK paths fail on Windows | Reproduced failure; pure Google modules zipped; relocated imports pass | Fixed and verified by isolated runtime test |
| Launcher arguments lose spaces | Process-argument quoting added; syntax parsed | Actual portable launches with spaced external evidence paths passed; argument fixture also passed |
| Natural microphone, noisy room, echo and device removal | Automated harnesses deliberately avoid human microphone capture | Requires external or human validation |
| Phoneme-aligned lip sync claim | Existing motion is spectral/envelope driven | Limitation explicitly documented; no phoneme claim |
| Missing original professional audit and baseline reports | Exact missing report types listed in continuation record | Unresolved; supply transferred originals to reconcile original issue IDs |
| Shipped asset/font/runtime provenance | Available notices and unresolved families recorded in distribution ledger | Partially addressed; public redistribution closure unresolved |
| Fresh package/source agreement and GitHub checkpoint | New branch codex/lumen-release-validation-20261022 requested by the user; player and backend checkpoints retained separately | New branch pushed; exact player-input overlay, backend hashes and portable manifest verified; remaining gates are listed in the owner report |

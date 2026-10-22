# Current acceptance matrix — 22 October 2026

The original audit matrix was not transferred, so these are request-derived
acceptance rows rather than invented original audit identifiers. Current starting
source is f0c5701 plus the explicitly documented continuation changes. Exact
player build GUIDs and run results belong in the new acceptance receipts.

| Problem / requirement | Change or current evidence | Status / remaining check |
| --- | --- | --- |
| Kai rear trousers intersect coat | 796 hidden trouser vertices tucked; baseline/final four-angle previews retained | Fixed, awaiting animated-pose acceptance |
| Aurelius trousers intersect tunic | 857 hidden trouser vertices tucked; baseline/final views retained | Fixed, awaiting animated-pose acceptance |
| Maya white cap fragment | Removed 26 cap UV-island triangles; extended existing crown after seam was observed | Fixed, awaiting animated-pose acceptance |
| Six-character rig/material regressions | All body inventories: 80 bones, 175 shapes, zero invalid materials | Partially addressed; runtime animations/speech still required |
| Authoring paths and missing Maya mask | Project-relative/configurable output; tracked UV mask and guarded recipe | Fixed, awaiting Blender regeneration on a machine with Blender |
| Stale Editor/player script layouts | First candidate failed with explicit layout mismatch; fresh Editor recompile and stale-source guard added | Fixed, awaiting successful fresh build |
| Python backend health | 86 tests passed; pip check clean | Fixed and verified for tested backend scope |
| Gemini credential/model access | Authentication accepted; real Live audio received | Fixed and verified at provider layer; player transport still required |
| Home/world portals, following and stationary looking | Existing functional harness retained | Awaiting exact-build functional run |
| Rome market/arena actual arrival | Existing functional/live harness checks movement outcomes | Awaiting exact-build runs |
| Café ordering/cancellation/restart/collection and advertised routes | Existing ordering and navigation code retained | Awaiting exact-build functional and visual review |
| Classroom all models, Anatomy vs Atom, unsupported requests and inspection | Existing functional harness retained | Awaiting exact-build run |
| Notes, coaching and offline behavior | Existing functional harness retained | Awaiting exact-build run |
| Responsive panels at four display sizes and largest text | Existing checks record actual Screen dimensions and overflow | Awaiting exact-build captures and review |
| Live memory, restart, cancellation and continuity | Owned-backend runner uses the dedicated restart window; 14-question harness retained | Awaiting real-provider player run |
| Rapid/partner/world changes, stale outputs and provider failure recovery | Existing conversation/image harnesses and lifecycle implementation retained | Awaiting targeted player checks; no blanket pass |
| Image retry before connection | Previously fixed source retained; functional harness checks it | Awaiting exact-build fixture and real-provider results separately |
| Images: relevance, opt-in Café, negative intent and stale topics | Existing bounded real-image harness retained | Awaiting exact-build image run and visual review |
| Performance metric conflation | Analyzer reports engine interval, CPU, GPU and callback gap separately | Fixed, awaiting telemetry run; targets/protocol absent |
| Unavailable render counters / shutdown flushing | No samples become -1; close is idempotent and also called on quit | Fixed, awaiting release telemetry inspection |
| Thirty-cycle memory/resource stability | Removed forced collection/unload; added persistent/audio/controller inventories | Fixed instrumentation, awaiting 30 cycles and trend review |
| Portable packaging uses old machine paths | Explicit build/output paths, optional model/runtime paths and copied Python closure | Partially addressed; full package launch pending |
| Deep Google SDK paths fail on Windows | Reproduced failure; pure Google modules zipped; relocated imports pass | Fixed and verified by isolated runtime test |
| Launcher arguments lose spaces | Process-argument quoting added; syntax parsed | Fixed, awaiting full launch with spaced acceptance paths |
| Natural microphone, noisy room, echo and device removal | Automated harnesses deliberately avoid human microphone capture | Requires external or human validation |
| Phoneme-aligned lip sync claim | Existing motion is spectral/envelope driven | Limitation explicitly documented; no phoneme claim |
| Missing original professional audit and baseline reports | Exact missing report types listed in continuation record | Unresolved; supply transferred originals to reconcile original issue IDs |
| Shipped asset/font/runtime provenance | Available notices and unresolved families recorded in distribution ledger | Partially addressed; public redistribution closure unresolved |
| Fresh package/source agreement and GitHub checkpoint | Authorized branch retained; no force-push or public release | Awaiting build, tests, package, final diff review and push |

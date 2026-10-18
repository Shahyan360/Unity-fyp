# Live educational image display: research only

Reviewed 19 October 2026. No image retrieval feature has been implemented; user requested fixing existing interactions first.

Recommended design: use a local 3D exhibit when supported. Otherwise let the teacher request a subject-specific image from the backend, resolve the entity (for example Paris Eiffel Tower rather than a similarly named attraction), retrieve a small set of eligible images, and show one on a world-space classroom display. Keep speech responsive while retrieval runs. Attach source page, title, creator and license; label photographs versus diagrams and reconstructions. Only say the image is visible after Unity confirms loading it. Allow dismiss, enlarge and compare. During explanations, offer a relevant visual rather than replacing the current subject unexpectedly.

Wikimedia Commons is the recommended starting provider for broad educational topics. Its MediaWiki API supports search and image information, thumbnail URLs and extended metadata including author/license fields. Metadata can be incomplete: reject uncertain reuse permissions rather than treating a search result as licensed. No special search API key is needed for public Commons reads; use a descriptive User-Agent and respectful limits.

NASA Image and Video Library is a useful additional source for space/science. Its REST endpoints provide search and asset metadata. Preserve credits and consult item-level usage restrictions; do not assume every third-party item is unrestricted.

Brave Image Search is a possible wider-web fallback requiring its own API credential and plan. It supplies image results and strict SafeSearch by default. Search access does not grant image reuse rights; restrict the selected assets to sources with acceptable reuse terms.

Google Custom Search JSON API should not be a new dependency: Google says it is closed to new customers, with existing customers required to transition by January 1, 2027. A Gemini API key alone is not a general Google Images API credential. Do not scrape Google Images result HTML or let a model invent download URLs.

Implementation boundaries for a later approved feature: server-side provider credentials, bounded cache on Drive D, licensed/source-backed results, HTTPS allowlists and public-address validation, image byte/dimension limits, reject redirects to private addresses, decode only supported image formats, timeouts/cancellation when the topic changes, no raw web instructions added to privileged prompts, and graceful unavailable/no-match messages. Prefer one relevant image over a distracting unsolicited slideshow. This is a proposed flow, not a claim of current functionality or latency.

Primary references:
- https://www.mediawiki.org/wiki/API:Imageinfo
- https://commons.wikimedia.org/wiki/Commons:API/MediaWiki
- https://images.nasa.gov/docs/images.nasa.gov_api_docs.pdf
- https://api-dashboard.search.brave.com/app/documentation/image-search
- https://developers.google.com/custom-search/v1/overview

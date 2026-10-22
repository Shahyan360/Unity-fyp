"""Attributed educational topic images. Only Wikimedia API-selected files are fetched."""
from __future__ import annotations

import hashlib
import html
import io
import json
import os
from pathlib import Path
import re
import threading
import time
from urllib.parse import urlsplit

import requests
from PIL import Image, UnidentifiedImageError

API = "https://en.wikipedia.org/w/api.php"
HEADERS = {"User-Agent": "LumenEducation/1.0 (educational Unity project; https://github.com/Shahyan360/Unity-fyp)"}
MAX_DOWNLOAD = 6 * 1024 * 1024
MAX_CACHE = 256 * 1024 * 1024
Image.MAX_IMAGE_PIXELS = 8_000_000
_slots = threading.BoundedSemaphore(3)
_cache_lock = threading.Lock()


def clean(value, limit=400):
    return re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]*>", "", str(value or "")))).strip()[:limit]


def media_url(url):
    try:
        p = urlsplit(url)
        return (p.scheme == "https" and p.hostname in {"upload.wikimedia.org", "thumb.wikimedia.org"}
                and p.port in {None, 443} and not p.username and not p.password
                and p.path.startswith("/wikipedia/commons/"))
    except ValueError:
        return False


def eligible(metadata):
    license_name = clean(metadata.get("LicenseShortName", {}).get("value", ""), 100)
    code = clean(metadata.get("License", {}).get("value", ""), 100).lower()
    return bool(re.fullmatch(r"cc-by(?:-sa)?-\d\.\d", code) or code in {"cc0", "pd", "pdm"}
                or license_name.lower() in {"public domain", "cc0", "cc0 1.0"})


def cache_root():
    # Keep runtime downloads out of the project/package; allow an explicit cache.
    return Path(os.environ.get("LUMEN_IMAGE_CACHE", str(Path(os.getenv("LOCALAPPDATA", str(Path.home() / ".cache"))) / "Lumen" / "LiveImages")))


def prune_cache(root):
    files = sorted(root.glob("*.json"), key=lambda p: p.stat().st_mtime, reverse=True)
    total = 0
    for index, meta in enumerate(files):
        jpg = meta.with_suffix(".jpg")
        total += jpg.stat().st_size if jpg.exists() else 0
        if index >= 256 or total > MAX_CACHE or time.time() - meta.stat().st_mtime > 7 * 86400:
            jpg.unlink(missing_ok=True)
            meta.unlink(missing_ok=True)


def _json(session, **params):
    response = session.get(API, params={"action": "query", "format": "json", **params}, timeout=(3, 6), allow_redirects=False)
    response.raise_for_status()
    if len(response.content) > 2_000_000:
        raise ValueError("Image metadata too large")
    data = response.json()
    if "error" in data:
        raise ValueError("Image provider unavailable")
    return list(data.get("query", {}).get("pages", {}).values())


def _download(session, url):
    if not media_url(url):
        raise ValueError("Unsupported image host")
    with session.get(url, stream=True, timeout=(3, 6), allow_redirects=False) as response:
        response.raise_for_status()
        if response.status_code != 200 or not response.headers.get("Content-Type", "").lower().startswith("image/"):
            raise ValueError("Image provider did not return an image")
        if int(response.headers.get("Content-Length", "0")) > MAX_DOWNLOAD:
            raise ValueError("Image too large")
        chunks, length = [], 0
        for chunk in response.iter_content(65536):
            length += len(chunk)
            if length > MAX_DOWNLOAD:
                raise ValueError("Image too large")
            chunks.append(chunk)
    with Image.open(io.BytesIO(b"".join(chunks))) as image:
        if image.width * image.height > 8_000_000:
            raise ValueError("Image dimensions too large")
        image.load()
        image.thumbnail((1280, 1000))
        rgba = image.convert("RGBA")
        output = Image.new("RGB", rgba.size, "white")
        output.paste(rgba, mask=rgba.getchannel("A"))
        stream = io.BytesIO()
        output.save(stream, format="JPEG", quality=88)
        return stream.getvalue(), output.width, output.height


def topic_query(query):
    """Remove presentation instructions without searching for their biographies."""
    query = clean(query, 160)
    query = re.split(r"\s+and\s+(?:explain|describe|tell)\b", query, maxsplit=1, flags=re.I)[0]
    query = re.sub(r"^(?:(?:please|show|me|a|an|the)\s+)+", "", query, flags=re.I)
    query = re.sub(r"^(?:picture|image|photo|diagram)\s+of\s+", "", query, flags=re.I)
    query = re.sub(r"\s+(?:model|picture|image|diagram)$", "", query, flags=re.I)
    return query.strip()


def topic_matches(query, title):
    # Search rank alone can return a scientist's portrait for a molecule request.
    # Require a topical title word; synonyms may honestly yield no suitable image.
    ignored = {"the", "a", "an", "of", "and", "for", "to", "in", "is", "what", "how",
               "please", "show", "me", "explain", "describe", "simply", "model", "image", "picture"}
    words = set(re.findall(r"\w+", query.casefold())) - ignored
    return bool(words & set(re.findall(r"\w+", title.casefold())))


def search_image(query):
    query = topic_query(query)
    if not query or len(query) < 2:
        return {"ok": False, "message": "Please name the topic you want to see."}
    identifier = hashlib.sha256(query.casefold().encode("utf-8")).hexdigest()
    root = cache_root()
    root.mkdir(parents=True, exist_ok=True)
    path = root / (identifier + ".json")
    with _cache_lock:
        if path.exists() and path.with_suffix(".jpg").exists() and time.time() - path.stat().st_mtime < 7 * 86400:
            try:
                value = json.loads(path.read_text(encoding="utf-8"))
                if (value.get("ok") and value.get("image_path") == "/images/file/" + identifier
                        and topic_matches(query, value.get("title", ""))):
                    return {**value, "cached": True}
            except (ValueError, OSError):
                pass
    if not _slots.acquire(blocking=False):
        return {"ok": False, "message": "Image search is busy. Please try again shortly."}
    try:
        with requests.Session() as session:
            session.headers.update(HEADERS)
            pages = _json(session, generator="search", gsrsearch=query, gsrnamespace=0,
                          gsrlimit=3, prop="pageimages|pageprops", piprop="name", ppprop="disambiguation")
            for page in sorted(pages, key=lambda p: p.get("index", 99)):
                if ("disambiguation" in page.get("pageprops", {}) or not page.get("pageimage")
                        or not topic_matches(query, page.get("title", ""))):
                    continue
                infos = _json(session, titles="File:" + page["pageimage"], prop="imageinfo",
                              iiprop="url|extmetadata", iiurlwidth=960,
                              iiextmetadatafilter="Artist|LicenseShortName|License|LicenseUrl|ImageDescription|Attribution|Credit")
                info = next((p["imageinfo"][0] for p in infos if p.get("imageinfo")), None)
                if not info or not eligible(info.get("extmetadata", {})):
                    continue
                url = info.get("thumburl", "")
                if not media_url(url):
                    continue
                try:
                    jpeg, width, height = _download(session, url)
                except (requests.RequestException, ValueError, UnidentifiedImageError, Image.DecompressionBombError, OSError):
                    continue
                meta = info["extmetadata"]
                source = info.get("descriptionurl", "")
                if urlsplit(source).hostname != "commons.wikimedia.org":
                    continue
                def value(name, limit=400):
                    return clean(meta.get(name, {}).get("value", ""), limit)
                result = {"ok": True, "query": query, "title": clean(page["title"], 160),
                          "description": value("ImageDescription", 300), "author": value("Artist", 220) or "See source for creator",
                          "credit": value("Attribution", 300) or value("Credit", 300),
                          "license": value("LicenseShortName", 100), "license_url": value("LicenseUrl", 300),
                          "source_url": source, "image_path": "/images/file/" + identifier,
                          "width": width, "height": height, "cached": False,
                          "modification": "Resized and converted to JPEG for display; transparent backgrounds composited on white."}
                with _cache_lock:
                    path.with_suffix(".jpg.tmp").write_bytes(jpeg)
                    path.with_suffix(".jpg.tmp").replace(path.with_suffix(".jpg"))
                    path.with_suffix(".json.tmp").write_text(json.dumps(result, ensure_ascii=False), encoding="utf-8")
                    path.with_suffix(".json.tmp").replace(path)
                    prune_cache(root)
                return result
        return {"ok": False, "query": query, "message": "No suitable licensed topic image was found. Try a more specific topic."}
    except (requests.RequestException, ValueError, OSError):
        return {"ok": False, "query": query, "message": "Image search is unavailable. You can continue the conversation and try again."}
    finally:
        _slots.release()


def register_images(app):
    from flask import jsonify, request, send_file, abort

    @app.post("/images/search")
    def image_search():
        from gemini_live import allowed_origin
        if not allowed_origin(request.headers.get("Origin", "")):
            return jsonify(ok=False, message="Only local clients may request images."), 403
        data = request.get_json(silent=True) or {}
        if not isinstance(data, dict) or not isinstance(data.get("query"), str) or len(data["query"]) > 160:
            return jsonify(ok=False, message="Provide a topic of at most 160 characters."), 400
        return jsonify(search_image(data["query"]))

    @app.get("/images/file/<identifier>")
    def image_file(identifier):
        if not re.fullmatch(r"[a-f0-9]{64}", identifier):
            abort(404)
        path = cache_root() / (identifier + ".jpg")
        if not path.is_file():
            abort(404)
        return send_file(path, mimetype="image/jpeg", max_age=86400, conditional=True)

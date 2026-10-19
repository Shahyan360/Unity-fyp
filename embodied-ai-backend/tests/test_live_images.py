import io
import json
import time
import hashlib

import pytest
import requests
from PIL import Image

import live_images as images
from app import create_app
from gemini_live import live_config


@pytest.mark.parametrize("url", ["http://upload.wikimedia.org/wikipedia/commons/a.jpg", "https://upload.wikimedia.org.evil.test/wikipedia/commons/a.jpg", "https://127.0.0.1/wikipedia/commons/a.jpg", "https://upload.wikimedia.org:5000/wikipedia/commons/a.jpg", "https://user:pw@upload.wikimedia.org/wikipedia/commons/a.jpg", "https://upload.wikimedia.org/wikipedia/en/a.jpg", "file:///D:/a.jpg"])
def test_rejects_untrusted_media_urls(url):
    assert not images.media_url(url)


def test_reusable_licenses_only():
    for code in ("cc-by-sa-4.0", "cc-by-3.0", "cc0", "pd"):
        assert images.eligible({"License": {"value": code}})
    for code in ("fairuse", "all-rights-reserved", "", "cc-by-nc-4.0"):
        assert not images.eligible({"License": {"value": code}})


def test_html_is_plain_text_not_executable():
    assert images.clean('<a href="bad">Creator &amp; Co</a>') == "Creator & Co"


def test_search_validates_request_and_origin():
    client = create_app({"TESTING": True}).test_client()
    for body in ({}, {"query": 42}, {"query": "a" * 161}, ["DNA"]):
        assert client.post("/images/search", json=body).status_code == 400
    assert client.post("/images/search", json={"query": "DNA"}, headers={"Origin": "https://evil.test"}).status_code == 403
    assert client.get("/images/file/not-a-cache-key").status_code == 404


def test_cache_hit_never_needs_the_network(monkeypatch, tmp_path):
    monkeypatch.setenv("LUMEN_IMAGE_CACHE", str(tmp_path))
    key = hashlib.sha256(b"dna").hexdigest()
    meta = {"ok": True, "query": "DNA", "image_path": "/images/file/" + key, "title": "DNA"}
    (tmp_path / (key + ".json")).write_text(json.dumps(meta), encoding="utf-8")
    (tmp_path / (key + ".jpg")).write_bytes(b"test")
    monkeypatch.setattr(requests, "Session", lambda: pytest.fail("Cache hit made a network request"))
    assert images.search_image("DNA")["cached"]
    client = create_app({"TESTING": True}).test_client()
    response = client.get("/images/file/" + key)
    assert response.status_code == 200 and response.content_type == "image/jpeg"


def test_unavailable_provider_is_an_honest_failure(monkeypatch, tmp_path):
    monkeypatch.setenv("LUMEN_IMAGE_CACHE", str(tmp_path))
    def fail(*args, **kwargs):
        raise requests.ConnectionError("network unavailable")
    monkeypatch.setattr(images, "_json", fail)
    result = images.search_image("electrical transformer")
    assert not result["ok"] and "unavailable" in result["message"]


def test_empty_results_do_not_reuse_previous_image(monkeypatch, tmp_path):
    monkeypatch.setenv("LUMEN_IMAGE_CACHE", str(tmp_path))
    monkeypatch.setattr(images, "_json", lambda *args, **kwargs: [])
    assert not images.search_image("unfindable topic")["ok"]


def test_oversized_decoded_image_is_rejected_without_server_error(monkeypatch, tmp_path):
    monkeypatch.setenv("LUMEN_IMAGE_CACHE", str(tmp_path))
    def metadata(session, **params):
        if "generator" in params:
            return [{"title": "DNA", "pageimage": "DNA.png", "index": 1}]
        return [{"imageinfo": [{"thumburl": "https://upload.wikimedia.org/wikipedia/commons/DNA.png", "extmetadata": {"License": {"value": "cc0"}}}]}]
    monkeypatch.setattr(images, "_json", metadata)
    def bomb(*args, **kwargs):
        raise Image.DecompressionBombError("oversized")
    monkeypatch.setattr(images, "_download", bomb)
    assert not images.search_image("DNA")["ok"]


def test_decode_limits_and_alpha_conversion():
    data = io.BytesIO()
    Image.new("RGBA", (20, 10), (0, 0, 0, 0)).save(data, "PNG")
    class Response:
        status_code = 200
        headers = {"Content-Type": "image/png"}
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def raise_for_status(self): pass
        def iter_content(self, size): yield data.getvalue()
    class Session:
        def get(self, *args, **kwargs):
            assert kwargs["allow_redirects"] is False
            return Response()
    jpeg, width, height = images._download(Session(), "https://upload.wikimedia.org/wikipedia/commons/a.png")
    decoded = Image.open(io.BytesIO(jpeg))
    assert (width, height) == (20, 10) and decoded.format == "JPEG"
    assert decoded.getpixel((0, 0)) == (255, 255, 255)


def test_cache_evicts_old_files(monkeypatch, tmp_path):
    import os
    old = tmp_path / ("a" * 64 + ".json")
    old.write_text("{}", encoding="utf-8")
    old.with_suffix(".jpg").write_bytes(b"image")
    os.utime(old, (time.time() - 8 * 86400,) * 2)
    images.prune_cache(tmp_path)
    assert not old.exists() and not old.with_suffix(".jpg").exists()


def test_images_automatically_accompany_questions_except_cafe():
    for scene in ("general_assistant", "classroom", "ancient_rome", "museum"):
        text = live_config(scene, "kai")["system_instruction"]
        assert "EVERY visitor question" in text and "electrical power transformer" in text
        assert "Never promise arbitrary models or live internet images" not in text


def test_cafe_images_are_opt_in_for_helpers_and_guests():
    for speaker, person in (("maya", ""), ("guest1", "Alex")):
        config = live_config("cafe", speaker, person)
        assert "do NOT automatically show images" in config["system_instruction"]
        assert "For EVERY visitor question" not in config["system_instruction"]
        tool = next(t for t in config["tools"][0]["function_declarations"] if t["name"] == "show_image")
        assert "ONLY on an explicit request" in tool["description"]

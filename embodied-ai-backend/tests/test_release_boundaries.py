from app import LocalStore, create_app
from gemini_live import live_config


def test_idle_sessions_expire_without_reusing_private_history():
    now = [100.0]
    store = LocalStore(ttl_seconds=10, clock=lambda: now[0])
    first, session = store.create("cafe")
    session.history.append({"role": "user", "text": "private"})
    now[0] = 111.0
    second, replacement = store.get_or_create(first, "cafe")
    assert second != first and replacement.history == []
    assert first not in store.sessions


def test_session_quota_evicts_oldest_and_keeps_recent_session():
    now = [100.0]
    store = LocalStore(max_sessions=2, clock=lambda: now[0])
    first, _ = store.create("classroom")
    now[0] += 1
    second, _ = store.create("museum")
    now[0] += 1
    third, _ = store.create("cafe")
    assert set(store.sessions) == {second, third}
    assert first not in store.sessions


def test_desktop_api_rejects_external_browser_origin_but_accepts_native():
    client = create_app({"TESTING": True}).test_client()
    assert client.get("/scenarios", headers={"Origin": "https://unrelated.example"}).status_code == 403
    assert client.get("/scenarios").status_code == 200
    assert client.get("/scenarios", headers={"Origin": "http://localhost:5000"}).status_code == 200


def test_reset_and_coaching_opt_out_are_explicit():
    client = create_app({"TESTING": True}).test_client()
    started = client.post("/coaching/start", json={"mode": "conversation"}).get_json()
    identifier = started["session_id"]
    assert client.post("/coaching/end", json={"session_id": identifier}).get_json()["active"] is False
    assert client.delete(f"/sessions/{identifier}").get_json()["reset"] is True
    assert client.get(f"/sessions/{identifier}/history").status_code == 404


def test_guides_receive_accessible_arena_and_separate_modern_cafe_contract():
    for speaker in ("kai", "orion", "theo", "aurelius", "lucius", "maya"):
        text = live_config("ancient_rome", speaker)["system_instruction"]
        assert "arena IS accessible" in text
        assert "modern cafe is a separate world" in text
        assert "only completed means arrival" in text
        assert "Explicit no images" in text

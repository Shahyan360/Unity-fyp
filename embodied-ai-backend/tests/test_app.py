from app import create_app


def test_health_and_scenarios_are_available():
    client = create_app({"TESTING": True}).test_client()
    assert client.get("/health").status_code == 200
    data = client.get("/scenarios").get_json()
    assert any(item["id"] == "academic_tutor" for item in data["scenarios"])


def test_chat_creates_private_session_and_returns_unity_contract():
    client = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()
    response = client.post("/chat", json={"query": "What is this?", "scenario": "academic_tutor", "object_name": "Atom Model"})
    assert response.status_code == 200
    data = response.get_json()
    assert data["response"] and data["session_id"]
    assert data["action"] in {"idle", "nod", "gesture", "point", "explain", "encourage", "listen", "celebrate"}
    assert data["offline"] is True
    history = client.get(f"/sessions/{data['session_id']}/history")
    assert history.status_code == 200
    assert len(history.get_json()["history"]) == 2


def test_social_coaching_starts_with_a_real_prompt():
    client = create_app({"TESTING": True}).test_client()
    response = client.post("/coaching/start", json={"mode": "interview"})
    assert response.status_code == 200
    assert "achievement" in response.get_json()["prompt"].lower()

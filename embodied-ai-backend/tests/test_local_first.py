import json
import re
from app import create_app, detect_requested_scenario


def test_saved_cloud_key_does_not_select_cloud(monkeypatch):
    monkeypatch.setenv("GEMINI_API_KEY", "test-not-a-real-key")
    monkeypatch.delenv("LUMEN_CONVERSATION_PROVIDER", raising=False)
    config = create_app({"TESTING": True}).test_client().get("/conversation/config").get_json()
    assert config["provider"] == "local"
    assert config["cloud_answers"] is False


def test_hybrid_uses_local_text_and_gemini_voice(monkeypatch):
    monkeypatch.setenv("LUMEN_CONVERSATION_PROVIDER", "hybrid")
    config = create_app({"TESTING": True}).test_client().get("/conversation/config").get_json()
    assert config["provider"] == "hybrid"
    assert config["answer_engine"] == "ollama"
    assert config["voice_engine"] == "gemini-tts"
    assert config["recognition_engine"] == "faster-whisper"
    assert config["cloud_answers"] is False


def test_lobby_aliases_are_explicit_navigation():
    assert detect_requested_scenario("Take me to the main screen.") == "home"
    assert detect_requested_scenario("Could you take me back to the lobby?") == "home"
    assert detect_requested_scenario("Don't go to the lobby.") == ""
    assert detect_requested_scenario("What is in the lobby?") == ""
    assert detect_requested_scenario("Go to the museum near home.") == "museum"


def test_educational_answer_is_locally_generated_with_grounding(monkeypatch):
    calls = []
    class Upstream:
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def raise_for_status(self): pass
        def iter_lines(self, chunk_size):
            yield json.dumps({"message": {"content": "We see different portions of the Moon's sunlit half."}}).encode()
            yield b'{"done":true}'
    def post(url, **kwargs):
        assert url.endswith("/api/chat")
        calls.append(kwargs["json"])
        return Upstream()
    monkeypatch.setattr("app.requests.post", post)
    client = create_app({"TESTING": True}).test_client()
    response = client.post("/chat/stream", json={"query": "Why does the moon have phases?", "scenario": "classroom",
        "world_context": "Explicit shared preferences: visitor name=Robin; favourite topic=astronomy"})
    result = json.loads(response.data.splitlines()[-1])["result"]
    assert result["response_source"] == "ollama" and result["cloud_answer"] is False
    assert calls[0]["think"] is False
    assert "Robin" in calls[0]["messages"][0]["content"]
    assert result["next_scenario"] == ""


def test_actions_keep_deterministic_contract_without_waiting_for_llm(monkeypatch):
    def forbidden(*args, **kwargs): raise AssertionError("No inference needed for world travel")
    monkeypatch.setattr("app.requests.post", forbidden)
    response = create_app({"TESTING": True}).test_client().post("/chat/stream", json={"query": "take me to the museum", "scenario": "classroom"})
    result = json.loads(response.data)["result"]
    assert result["next_scenario"] == "museum"


def test_explicit_preference_recall_does_not_invent_guide_identity(monkeypatch):
    def forbidden(*args, **kwargs): raise AssertionError("Recall should read state")
    monkeypatch.setattr("app.requests.post", forbidden)
    response = create_app({"TESTING": True}).test_client().post("/chat/stream", json={
        "query": "What is my name and favourite topic?", "scenario": "general_assistant",
        "world_context": "Explicit shared preferences: visitor name=Robin; favourite topic=astronomy. This speaker's last four exchanges: none"})
    result = json.loads(response.data)["result"]
    assert result["response"] == "Your name is Robin. Your favourite topic is astronomy."
    assert result["response_source"] == "local-memory"


def test_grounded_hallucination_never_reaches_captions_or_speech(monkeypatch):
    wrong = "The Moon changes phases because Earth casts a shadow on it."

    class Upstream:
        def __enter__(self):
            return self

        def __exit__(self, *args):
            pass

        def raise_for_status(self):
            pass

        def iter_lines(self, chunk_size):
            yield json.dumps({"message": {"content": wrong}}).encode()
            yield b'{"done":true}'

    monkeypatch.setattr("app.requests.post", lambda *args, **kwargs: Upstream())
    client = create_app({"TESTING": True}).test_client()
    response = client.post("/chat/stream", json={
        "query": "What makes the moon look different throughout the month?",
        "scenario": "classroom",
    })
    packets = [json.loads(line) for line in response.data.splitlines()]
    assert [packet["event"] for packet in packets] == ["delta", "sentence", "done"]
    assert all(wrong not in json.dumps(packet) for packet in packets)
    result = packets[-1]["result"]
    assert result["response_source"] == "local-reference"
    assert "illuminated half" in result["response"]


def test_twenty_minute_study_plan_respects_total(monkeypatch):
    def forbidden(*args, **kwargs):
        raise AssertionError("The exact time allocation should be computed locally")

    monkeypatch.setattr("app.requests.post", forbidden)
    client = create_app({"TESTING": True}).test_client()
    result = json.loads(client.post("/chat/stream", json={
        "query": "Suggest a twenty-minute study routine with a short break.",
        "scenario": "general_assistant",
    }).data)["result"]
    assert result["response_source"] == "local-plan"
    assert [int(number) for number in re.findall(r"\d+", result["response"])] == [14, 3, 3]

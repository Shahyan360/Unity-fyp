import json
from app import create_app


def test_streamed_curated_reply_keeps_contract_and_character():
    app = create_app({"TESTING": True})
    response = app.test_client().post("/chat/stream", json={"query": "who are you", "scenario": "museum", "speaker_id": "orion"})
    packets = [json.loads(line) for line in response.data.splitlines()]
    assert packets[-1]["event"] == "done"
    result = packets[-1]["result"]
    assert result["speaker_name"] == "Orion" and "Orion" in result["response"]
    assert result["next_scenario"] == ""


def test_tokens_and_sentence_arrive_before_final_response(monkeypatch):
    class Upstream:
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def raise_for_status(self): pass
        def iter_lines(self, chunk_size):
            for text in ["A blue feather", " can tell a story.", " What caught your eye?"]:
                yield json.dumps({"message": {"content": text}}).encode()
            yield b'{"done":true}'
    calls = []
    def generate(url, **kwargs):
        calls.append(kwargs["json"])
        return Upstream()
    monkeypatch.setattr("app.requests.post", generate)
    client = create_app({"TESTING": True}).test_client()
    response = client.post("/chat/stream", json={"query": "A blue feather caught my eye today", "scenario": "museum", "speaker_id": "orion"}, buffered=False)
    iterator = iter(response.response)
    assert json.loads(next(iterator))["event"] == "delta"
    remaining = [json.loads(p) for p in iterator]
    assert any(p["event"] == "sentence" for p in remaining)
    result = remaining[-1]["result"]
    assert result["response_source"] == "ollama" and result["speaker_id"] == "orion"
    assert calls[0]["stream"] is True and "format" not in calls[0]
    history = client.get("/sessions/"+result["session_id"]+"/history").get_json()["history"]
    assert len(history) == 2


def test_stream_error_is_explicit_not_an_invented_answer(monkeypatch):
    import requests
    def broken(*args, **kwargs): raise requests.ConnectionError("offline")
    monkeypatch.setattr("app.requests.post", broken)
    response = create_app({"TESTING": True}).test_client().post("/chat/stream", json={"query": "A blue feather caught my eye today", "scenario": "museum"})
    packet = json.loads(response.data)
    assert packet["event"] == "error" and "did not finish" in packet["text"]


def test_transcribe_rejects_invalid_and_oversized_audio():
    client = create_app({"TESTING": True}).test_client()
    assert client.post("/transcribe", data=b"not a wav").status_code == 400
    assert client.post("/transcribe", data=b"x"*600001).status_code == 413
    assert client.get("/voice/health").get_json()["engine"] == "faster-whisper"


def test_appearance_personality_does_not_override_scenario():
    client = create_app({"TESTING": True}).test_client()
    a = client.post("/chat/stream", json={"query": "who are you", "scenario": "ancient_rome", "speaker_id": "kai"})
    b = client.post("/chat/stream", json={"query": "who are you", "scenario": "classroom", "speaker_id": "theo"})
    first, second = json.loads(a.data)["result"], json.loads(b.data)["result"]
    assert first["speaker_name"] == "Kai" and first["scenario"] == "ancient_rome"
    assert second["speaker_name"] == "Professor Theo" and second["scenario"] == "classroom"

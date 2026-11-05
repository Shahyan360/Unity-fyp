import base64
import io
import json
import wave
import threading
from concurrent.futures import ThreadPoolExecutor

import pytest
import requests

from app import create_app
from gemini_live import voices_for_person
from gemini_speech import synthesize


def audio_result(data=b"\x00\x01" * 2400, mime="audio/L16;codec=pcm;rate=24000"):
    return {"candidates": [{"content": {"parts": [{"inlineData": {
        "data": base64.b64encode(data).decode(), "mimeType": mime}}]}}]}


class Upstream:
    def __init__(self, result):
        self.result = result
    def raise_for_status(self):
        pass
    def json(self):
        return self.result
    def close(self):
        pass


def test_hybrid_voices_exact_local_answer_without_sending_question_or_history(monkeypatch):
    monkeypatch.setattr("gemini_speech.api_key", lambda: "test-key")
    calls = []
    def post(url, **kwargs):
        calls.append((url, kwargs))
        return Upstream(audio_result())
    monkeypatch.setattr("gemini_speech.requests.post", post)
    client = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "hybrid"}).test_client()
    text = "We see different fractions of the Moon's sunlit half."
    result = client.post("/speech/synthesize", json={"text": text, "speaker_id": "theo"})
    assert result.status_code == 200
    assert result.headers["X-Lumen-Speech-Engine"] == "gemini-tts"
    request = calls[0][1]["json"]
    assert request["contents"] == [{"parts": [{"text": text}]}]
    assert set(request) == {"contents", "generationConfig"}
    assert request["generationConfig"]["speechConfig"]["voiceConfig"]["prebuiltVoiceConfig"]["voiceName"] == "Iapetus"
    with wave.open(io.BytesIO(result.data)) as wav:
        assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate(), wav.getnframes()) == (1, 2, 24000, 2400)


def test_hybrid_microphone_transcript_gets_answer_from_ollama(monkeypatch):
    calls = []
    class ModelReply:
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def raise_for_status(self): pass
        def iter_lines(self, chunk_size):
            yield json.dumps({"message": {"content": "Start by saying hello and asking their name."}}).encode()
            yield b'{"done":true}'
    def post(url, **kwargs):
        calls.append(url)
        assert url.startswith("http://127.0.0.1:11434/")
        return ModelReply()
    monkeypatch.setattr("app.requests.post", post)
    client = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "hybrid", "OLLAMA_URL": "http://127.0.0.1:11434"}).test_client()
    response = client.post("/chat/stream", json={"query": "Help me greet a new classmate.", "scenario": "cafe"})
    result = json.loads(response.data.splitlines()[-1])["result"]
    assert result["response_source"] == "ollama" and result["cloud_answer"] is False
    assert len(calls) == 1


def test_native_wav_is_not_wrapped_a_second_time(monkeypatch):
    output = io.BytesIO()
    with wave.open(output, "wb") as wav:
        wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(24000); wav.writeframes(b"\0\0" * 80)
    data = output.getvalue()
    monkeypatch.setattr("gemini_speech.api_key", lambda: "test-key")
    monkeypatch.setattr("gemini_speech.requests.post", lambda *args, **kwargs: Upstream(audio_result(data, "audio/wav")))
    assert synthesize("Hello.", "kai") == data


@pytest.mark.parametrize("result", [{}, audio_result(b"bad", "text/plain"), audio_result(b"x", "audio/L16")])
def test_invalid_provider_audio_fails_without_losing_local_answer(monkeypatch, result):
    monkeypatch.setattr("gemini_speech.api_key", lambda: "test-key")
    monkeypatch.setattr("gemini_speech.requests.post", lambda *args, **kwargs: Upstream(result))
    client = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "hybrid"}).test_client()
    response = client.post("/speech/synthesize", json={"text": "The answer stays in the chat."})
    assert response.status_code == 503
    assert "local answer" in response.get_json()["error"]


def test_provider_errors_are_sanitized_and_release_speech_slot(monkeypatch):
    monkeypatch.setattr("gemini_speech.api_key", lambda: "private-test-key")
    def fail(*args, **kwargs):
        raise requests.Timeout("private-test-key in a provider URL")
    monkeypatch.setattr("gemini_speech.requests.post", fail)
    client = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "hybrid"}).test_client()
    response = client.post("/speech/synthesize", json={"text": "Hello."})
    assert response.status_code == 503 and b"private-test-key" not in response.data
    monkeypatch.setattr("gemini_speech.requests.post", lambda *args, **kwargs: Upstream(audio_result()))
    assert client.post("/speech/synthesize", json={"text": "Retry."}).status_code == 200


def test_local_mode_never_uses_cloud_synthesis(monkeypatch):
    monkeypatch.setattr("gemini_speech.synthesize", lambda *args: pytest.fail("Cloud speech must stay off in local mode"))
    client = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "local"}).test_client()
    assert client.get("/speech/config").get_json()["engine"] == "piper"
    assert client.post("/speech/synthesize", json={"text": "Hello."}).status_code == 503


def test_new_speech_turn_can_start_while_an_old_request_finishes(monkeypatch):
    release = threading.Event()
    both_started = threading.Event()
    lock = threading.Lock()
    active = 0
    def hold(*args):
        nonlocal active
        with lock:
            active += 1
            if active == 2:
                both_started.set()
        assert release.wait(5)
        return b"audio fixture"
    monkeypatch.setattr("gemini_speech.synthesize", hold)
    app = create_app({"TESTING": True, "CONVERSATION_PROVIDER": "hybrid"})
    def post():
        with app.test_client() as client:
            return client.post("/speech/synthesize", json={"text": "Hello."}).status_code
    with ThreadPoolExecutor(max_workers=2) as pool:
        first, second = pool.submit(post), pool.submit(post)
        try:
            assert both_started.wait(3)
            assert post() == 503  # The upper bound still prevents unbounded requests.
        finally:
            release.set()
        assert first.result() == second.result() == 200


def test_character_voice_selection_is_stable():
    assert voices_for_person("kai") == "Puck"
    assert voices_for_person("orion") == "Charon"
    assert voices_for_person("", "Alex") == voices_for_person("", "Alex")

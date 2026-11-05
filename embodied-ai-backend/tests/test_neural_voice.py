import pytest

from app import create_app


@pytest.mark.parametrize("contents", ["{partial download", "{}", '{"speaker_id_map": []}'])
def test_invalid_optional_voice_config_does_not_disable_chat(tmp_path, monkeypatch, contents):
    voices = tmp_path / "voices"
    voices.mkdir()
    (voices / "en_GB-vctk-medium.onnx.json").write_text(contents, encoding="utf-8")
    monkeypatch.setenv("LUMEN_PIPER_HOME", str(tmp_path))
    client = create_app({"TESTING": True}).test_client()
    assert client.get("/speech/config").get_json()["ready"] is False
    assert client.post("/speech/synthesize", json={"text": "Hello"}).status_code == 503
    result = client.post("/chat", json={"query": "Who are you?", "scenario": "classroom"})
    assert result.status_code == 200
    assert result.get_json()["response_source"] == "local-introduction"

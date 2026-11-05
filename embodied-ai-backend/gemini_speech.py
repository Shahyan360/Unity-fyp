"""Gemini TTS reads application-authored replies; it never answers visitor questions."""
import base64
import io
import os
import re
import wave

import requests

from gemini_live import api_key, voices_for_person


def model_name():
    return os.getenv("LUMEN_GEMINI_TTS_MODEL", "gemini-3.8-flash-tts")


def synthesize(text, speaker_id="", object_name="", voice_name=""):
    key = api_key()
    model = model_name()
    if not key or not re.fullmatch(r"gemini-[a-zA-Z0-9._-]+", model):
        raise ValueError("Gemini speech is not configured.")
    voice = voices_for_person(speaker_id, object_name, voice_name)
    # Only the finished local answer crosses this boundary: no question, history,
    # world instructions, or tools are sent to the cloud speech model.
    response = requests.post(
        f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
        headers={"x-goog-api-key": key},
        json={"contents": [{"parts": [{"text": text}]}],
              "generationConfig": {"responseModalities": ["AUDIO"],
                  "speechConfig": {"voiceConfig": {"prebuiltVoiceConfig": {"voiceName": voice}}}}},
        timeout=(5, 30))
    try:
        response.raise_for_status()
        result = response.json()
        parts = (result.get("candidates") or [{}])[0].get("content", {}).get("parts", [])
        audio = next((part["inlineData"] for part in parts if part.get("inlineData", {}).get("data")), None)
        if audio is None:
            raise ValueError("Gemini returned no speech audio.")
        encoded = audio["data"]
        if not isinstance(encoded, str) or len(encoded) > 16_000_000:
            raise ValueError("Speech exceeds its size limit.")
        data = base64.b64decode(encoded, validate=True)
        mime = str(audio.get("mimeType", "")).lower()
        if mime.split(";")[0] in {"audio/wav", "audio/x-wav"}:
            with wave.open(io.BytesIO(data)) as wav:
                if wav.getnchannels() != 1 or wav.getsampwidth() != 2 or wav.getnframes() == 0:
                    raise ValueError("Unsupported speech format.")
            return data
        if mime.split(";")[0] not in {"audio/l16", "audio/pcm"} or not data or len(data) % 2:
            raise ValueError("Unsupported speech format.")
        match = re.search(r"\brate=(\d+)", mime)
        rate = int(match[1]) if match else 24000
        if not 8000 <= rate <= 48000:
            raise ValueError("Unsupported speech sample rate.")
        output = io.BytesIO()
        with wave.open(output, "wb") as wav:
            wav.setnchannels(1)
            wav.setsampwidth(2)
            wav.setframerate(rate)
            wav.writeframes(data)
        return output.getvalue()
    finally:
        response.close()

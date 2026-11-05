"""Configured speech: resident local voice or bounded Gemini TTS requests."""
import json
import atexit
import os
import queue
import subprocess
import threading
import uuid
import wave
import requests
from pathlib import Path

from flask import jsonify, request, Response


def register_neural_voice(app):
    root = Path(os.getenv("LUMEN_PIPER_HOME", str(Path(__file__).resolve().parent.parent / "Runtime" / "Piper")))
    executable = root / "piper" / "piper.exe"
    model = root / "voices" / "en_GB-vctk-medium.onnx"
    output = Path(__file__).resolve().parent / "runtime" / ("neural-speech-" + uuid.uuid4().hex + ".wav")
    # Voice files are optional, and an interrupted installation must not stop chat.
    speakers = {}
    try:
        config = json.loads(Path(str(model) + ".json").read_text(encoding="utf-8"))
        mapping = config.get("speaker_id_map") if isinstance(config, dict) else None
        if isinstance(mapping, dict) and mapping and all(
            isinstance(label, str) and type(number) is int and number >= 0
            for label, number in mapping.items()
        ):
            speakers = mapping
    except (OSError, ValueError):
        pass
    lock = threading.Lock()
    # Allow a new turn while a cancelled client's cloud request finishes.
    cloud_slots = threading.BoundedSemaphore(2)
    state = {"process": None, "reader": None, "ready": False}
    completed = queue.Queue(maxsize=4)

    def stop():
        process = state["process"]
        state.update(process=None, ready=False)
        if process is not None and process.poll() is None:
            process.kill()
            process.wait(timeout=3)
        reader = state.get("reader")
        if reader is not None:
            reader.join(timeout=1)
        while not completed.empty():
            try:
                completed.get_nowait()
            except queue.Empty:
                break
        output.unlink(missing_ok=True)

    def read_output(process):
        for line in process.stdout:
            if line.strip():
                try:
                    completed.put_nowait(line.strip())
                except queue.Full:
                    return

    def synthesize(text, speaker="p226"):
        process = state["process"]
        process.stdin.write(json.dumps({"text": text, "speaker_id": speakers.get(speaker, 95)}) + "\n")
        process.stdin.flush()
        completed.get(timeout=12)
        return output.read_bytes()

    def warm():
        if not speakers or not executable.is_file() or not model.is_file():
            return
        with lock:
            try:
                output.parent.mkdir(parents=True, exist_ok=True)
                state["process"] = subprocess.Popen(
                    [str(executable), "--model", str(model), "--json-input", "--output_file", str(output)],
                    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                    text=True, encoding="utf-8", creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                    cwd=str(executable.parent))
                reader = threading.Thread(target=read_output, args=(state["process"],), daemon=True)
                state["reader"] = reader
                reader.start()
                synthesize("Ready.")
                state["ready"] = True
            except (OSError, queue.Empty, subprocess.SubprocessError):
                stop()

    @app.get("/speech/config")
    def config():
        if app.config.get("CONVERSATION_PROVIDER") == "hybrid":
            from gemini_speech import api_key, model_name
            return jsonify(engine="gemini-tts", ready=bool(api_key()), model=model_name())
        return jsonify(engine="piper", ready=state["ready"])

    @app.post("/speech/synthesize")
    def speech():
        payload = request.get_json(silent=True)
        text = payload.get("text") if isinstance(payload, dict) else None
        if not isinstance(text, str) or not text.strip() or len(text) > 2000:
            return jsonify(error="Supply 1 to 2000 text characters."), 400
        if app.config.get("CONVERSATION_PROVIDER") == "hybrid":
            if not cloud_slots.acquire(blocking=False):
                return jsonify(error="Speech synthesis is busy."), 503
            try:
                from gemini_speech import synthesize as cloud_synthesize
                audio = cloud_synthesize(text, str(payload.get("speaker_id", ""))[:80],
                    str(payload.get("object_name", ""))[:100], str(payload.get("voice_name", ""))[:40])
                return Response(audio, mimetype="audio/wav", headers={"X-Lumen-Speech-Engine": "gemini-tts"})
            except (requests.RequestException, ValueError, KeyError, TypeError, wave.Error, EOFError):
                # Provider exception messages can contain request URLs or keys.
                return jsonify(error="Gemini speech is unavailable. The local answer is still available."), 503
            finally:
                cloud_slots.release()
        if not state["ready"] or not lock.acquire(blocking=False):
            return jsonify(error="Neural voice is warming or busy."), 503
        try:
            return Response(synthesize(text, str(payload.get("speaker", "p226"))), mimetype="audio/wav",
                            headers={"X-Lumen-Speech-Engine": "piper"})
        except (OSError, queue.Empty, subprocess.SubprocessError):
            stop()
            threading.Thread(target=warm, daemon=True).start()
            return jsonify(error="Neural voice restarting."), 503
        finally:
            lock.release()

    atexit.register(stop)
    if not app.config.get("TESTING") and app.config.get("CONVERSATION_PROVIDER") != "hybrid":
        threading.Thread(target=warm, daemon=True).start()

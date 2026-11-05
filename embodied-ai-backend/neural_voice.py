"""Resident local neural voice. One bounded synthesis at a time."""
import json
import atexit
import os
import queue
import subprocess
import threading
import uuid
from pathlib import Path

from flask import jsonify, request, Response


def register_neural_voice(app):
    root = Path(os.getenv("LUMEN_PIPER_HOME", str(Path(__file__).resolve().parent.parent / "Runtime" / "Piper")))
    executable = root / "piper" / "piper.exe"
    model = root / "voices" / "en_GB-vctk-medium.onnx"
    output = Path(__file__).resolve().parent / "runtime" / ("neural-speech-" + uuid.uuid4().hex + ".wav")
    speakers = json.loads(Path(str(model) + ".json").read_text(encoding="utf-8"))["speaker_id_map"] if Path(str(model)+".json").is_file() else {}
    lock = threading.Lock()
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
        if not executable.is_file() or not model.is_file():
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
        return jsonify(engine="piper", ready=state["ready"])

    @app.post("/speech/synthesize")
    def speech():
        payload = request.get_json(silent=True)
        text = payload.get("text") if isinstance(payload, dict) else None
        if not isinstance(text, str) or not text.strip() or len(text) > 2000:
            return jsonify(error="Supply 1 to 2000 text characters."), 400
        if not state["ready"] or not lock.acquire(blocking=False):
            return jsonify(error="Neural voice is warming or busy."), 503
        try:
            return Response(synthesize(text, str(payload.get("speaker", "p226"))), mimetype="audio/wav")
        except (OSError, queue.Empty, subprocess.SubprocessError):
            stop()
            threading.Thread(target=warm, daemon=True).start()
            return jsonify(error="Neural voice restarting."), 503
        finally:
            lock.release()

    atexit.register(stop)
    if not app.config.get("TESTING"):
        threading.Thread(target=warm, daemon=True).start()

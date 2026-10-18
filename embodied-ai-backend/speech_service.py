"""Local, bounded PCM transcription. Model/runtime/cache live on drive D."""
import io
import os
import threading
import time
import wave
from pathlib import Path


class SpeechService:
    def __init__(self):
        self.model_path = os.getenv("LUMEN_STT_MODEL", "D:/LumenTools/VoiceModels/base.en")
        self.model = None
        self.error = ""
        self.lock = threading.Lock()

    def health(self):
        return {"engine": "faster-whisper", "model": self.model_path,
                "installed": Path(self.model_path, "model.bin").is_file(),
                "ready": self.model is not None, "loading": self.lock.locked() and self.model is None,
                "error": self.error, "language": "en"}

    def load(self):
        if self.model is None:
            from faster_whisper import WhisperModel
            if not Path(self.model_path, "model.bin").is_file():
                raise RuntimeError("Local speech model is not installed on D. Run Tools/Voice/setup_voice.py.")
            self.model = WhisperModel(self.model_path, device="cpu", compute_type="int8", cpu_threads=2, num_workers=1)

    def warm(self):
        with self.lock:
            try:
                self.load()
            except (ImportError, RuntimeError) as exc:
                self.error = str(exc)

    def transcribe(self, raw):
        started = time.perf_counter()
        with wave.open(io.BytesIO(raw), "rb") as wav:
            if wav.getnchannels() != 1 or wav.getsampwidth() != 2 or wav.getframerate() != 16000:
                raise ValueError("Send mono 16-bit PCM WAV at 16000 Hz.")
            duration = wav.getnframes() / 16000
            if not .2 <= duration <= 16:
                raise ValueError("Speech segments must be between 0.2 and 16 seconds.")
            pcm = wav.readframes(wav.getnframes())
        if not self.lock.acquire(blocking=False):
            return {"error": "Recognizer busy; retry the captured utterance.", "retry": True}, 429
        try:
            import numpy as np
            self.load()
            audio = np.frombuffer(pcm, dtype=np.int16).astype(np.float32) / 32768
            if float(np.sqrt(np.mean(audio * audio))) < .001:
                return {"text": "", "duration": duration, "recognition_ms": 0}, 200
            segments, _ = self.model.transcribe(audio, language="en", beam_size=3,
                condition_on_previous_text=False, temperature=0, vad_filter=True,
                vad_parameters={"min_silence_duration_ms": 300},
                initial_prompt="A visitor speaking to Kai, Orion, Professor Theo, Maya, Aurelius or Lucius in a museum, Ancient Rome, science classroom or cafe.")
            accepted = [s.text.strip() for s in segments if s.no_speech_prob < .65 and s.avg_logprob > -1.15]
            self.error = ""
            return {"text": " ".join(accepted), "duration": duration,
                    "recognition_ms": round((time.perf_counter() - started) * 1000, 1)}, 200
        except (ImportError, RuntimeError) as exc:
            self.error = str(exc)
            return {"error": self.error}, 503
        finally:
            self.lock.release()

"""Run with D:/LumenTools/VoiceRuntime/Scripts/python.exe. No C-drive model cache."""
import os
from pathlib import Path

os.environ["HF_HOME"] = "D:/LumenProject/Cache/huggingface"
os.environ["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1"
from huggingface_hub import snapshot_download

folder = Path("D:/LumenTools/VoiceModels/base.en")
folder.mkdir(parents=True, exist_ok=True)
snapshot_download("Systran/faster-whisper-base.en", local_dir=str(folder),
                  allow_patterns=["model.bin", "config.json", "tokenizer.json", "vocabulary.*", "preprocessor_config.json", "README.md"])
print("LOCAL_SPEECH_READY", folder)

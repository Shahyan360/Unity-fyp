# Embodied AI local backend

This is the completed local service for the Unity project. It contains no cloud AI dependency: Unity calls Flask on `127.0.0.1`, and Flask calls the locally installed Ollama model.

## Start

```powershell
cd embodied-ai-backend
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
ollama pull llama3.2:3b
python app.py
```

If a smaller or larger local model is preferred, set `OLLAMA_MODEL` before starting the service. The default `llama3.2:3b` offers a better balance of response quality and local hardware requirements than the original 1B prototype. `llama3.2:1b` remains supported for 4 GB VRAM devices.

## API

- `GET /health` - local-service availability
- `GET /scenarios` - scenario catalog
- `POST /sessions` - starts an in-memory, private conversation
- `POST /chat` - unified text / object-aware conversation request
- `POST /coaching/start` - starts interview, presentation, or casual-conversation practice
- `GET /sessions/{id}/history` - retrieves the active session's in-memory history

When Ollama is not running, `/chat` returns a clearly flagged `offline: true` guidance response. This makes the Unity demo usable and diagnosable without fabricating an AI answer.

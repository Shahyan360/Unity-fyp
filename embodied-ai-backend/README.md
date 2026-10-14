# Embodied AI local backend

This is the completed local service for the Unity project. It contains no cloud AI dependency: Unity calls Flask on `127.0.0.1`, and Flask calls the locally installed Ollama model.

## Start

```powershell
cd embodied-ai-backend
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
ollama pull llama3.2:1b
python app.py
```

The default `llama3.2:1b` matches the lightweight model used by the current development setup. For more capable hardware, pull `llama3.2:3b` and set `OLLAMA_MODEL=llama3.2:3b` before starting Flask. If a model returns malformed JSON or no answer, the API flags the turn as offline and uses the scenario's curated local knowledge instead of returning an empty generic reply.

## API

- `GET /health` - local-service availability
- `GET /scenarios` - scenario catalog
- `POST /sessions` - starts an in-memory, private conversation
- `POST /chat` - unified text / object-aware conversation request
- `POST /coaching/start` - starts interview, presentation, or casual-conversation practice
- `GET /sessions/{id}/history` - retrieves the active session's in-memory history

When Ollama is not running, `/chat` returns a clearly flagged `offline: true` guidance response. This makes the Unity demo usable and diagnosable without fabricating an AI answer.

"""Local API for the Embodied AI Unity experience.

The service intentionally keeps all prompt construction and session data on the
user's machine.  Ollama is optional in development: when it is unavailable the
API returns an explicit, useful offline response rather than pretending a model
answer was generated.
"""
from __future__ import annotations

import json
import os
import re
import time
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import requests
from flask import Flask, jsonify, request
from flask_cors import CORS


BASE_DIR = Path(__file__).resolve().parent
DATA_DIR = BASE_DIR / "data"
MAX_HISTORY = 8
ALLOWED_EMOTIONS = {"neutral", "friendly", "thinking", "excited", "concerned", "confident"}
ALLOWED_ACTIONS = {"idle", "nod", "gesture", "point", "explain", "encourage", "listen", "celebrate"}
ALLOWED_INTENTS = {"answer", "object_explanation", "coach_feedback", "navigate", "conversation"}


SCENARIOS: dict[str, dict[str, Any]] = {
    "general_assistant": {
        "title": "Embodied AI Assistant",
        "role": "a concise, friendly local virtual assistant",
        "style": "warm, clear and practical",
        "scene": "HomeScene",
        "knowledge_file": "general.txt",
    },
    "academic_tutor": {
        "title": "Academic Tutor Lab",
        "role": "an encouraging science tutor who teaches through the nearby 3D models",
        "style": "accurate, structured, concise, and age-appropriate",
        "scene": "AcademicLabScene",
        "knowledge_file": "academic.txt",
    },
    "historical_guide": {
        "title": "Ancient World Explorer",
        "role": "an enthusiastic historical guide who distinguishes facts from interpretation",
        "style": "vivid but historically careful",
        "scene": "HistoricalExplorerScene",
        "knowledge_file": "history.txt",
    },
    "social_coach": {
        "title": "Social Practice Studio",
        "role": "a supportive, non-judgmental communication coach",
        "style": "specific, constructive, and emotionally safe",
        "scene": "SocialStudioScene",
        "knowledge_file": "social.txt",
    },
}


@dataclass
class Session:
    scenario: str
    created_at: float = field(default_factory=time.time)
    history: list[dict[str, str]] = field(default_factory=list)
    coaching_mode: str = ""
    coaching_turn: int = 0


class LocalStore:
    """In-memory private session store. No conversation is written to disk."""

    def __init__(self) -> None:
        self.sessions: dict[str, Session] = {}

    def create(self, scenario: str) -> tuple[str, Session]:
        identifier = uuid.uuid4().hex
        session = Session(scenario=scenario)
        self.sessions[identifier] = session
        return identifier, session

    def get_or_create(self, session_id: str | None, scenario: str) -> tuple[str, Session]:
        if session_id and session_id in self.sessions:
            session = self.sessions[session_id]
            if session.scenario != scenario:
                session.scenario = scenario
                session.history.clear()
            return session_id, session
        return self.create(scenario)


def read_knowledge(scenario: str, query: str, limit: int = 4) -> list[str]:
    file_name = SCENARIOS[scenario]["knowledge_file"]
    path = DATA_DIR / "knowledge" / file_name
    if not path.exists():
        return []
    chunks = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    terms = {word.lower() for word in re.findall(r"[a-zA-Z]{4,}", query)}
    ranked = sorted(
        ((sum(term in chunk.lower() for term in terms), index, chunk) for index, chunk in enumerate(chunks)),
        key=lambda item: (-item[0], item[1]),
    )
    selected = [chunk for score, _, chunk in ranked if score > 0][:limit]
    return selected or chunks[:limit]


def user_emotion(text: str) -> str:
    words = set(re.findall(r"[a-zA-Z']+", text.lower()))
    if words & {"anxious", "afraid", "worried", "nervous", "sad", "upset"}:
        return "concerned"
    if words & {"great", "excited", "happy", "thanks", "confident"}:
        return "excited"
    if words & {"confused", "stuck", "unclear", "difficult", "help"}:
        return "thinking"
    return "friendly"


def normalise_result(value: dict[str, Any], fallback_emotion: str, offline: bool = False) -> dict[str, Any]:
    response = str(value.get("response", "")).strip() or "I am ready to help."
    emotion = str(value.get("emotion", fallback_emotion)).lower().strip()
    action = str(value.get("action", "idle")).lower().strip()
    intent = str(value.get("intent", "answer")).lower().strip()
    navigation_target = str(value.get("navigation_target", "")).strip()[:80]
    return {
        "response": response[:1200],
        "emotion": emotion if emotion in ALLOWED_EMOTIONS else fallback_emotion,
        "action": action if action in ALLOWED_ACTIONS else "idle",
        "intent": intent if intent in ALLOWED_INTENTS else "answer",
        "navigation_target": navigation_target,
        "offline": offline,
    }


def extract_json(text: str) -> dict[str, Any] | None:
    try:
        value = json.loads(text)
        return value if isinstance(value, dict) else None
    except json.JSONDecodeError:
        match = re.search(r"\{.*\}", text, flags=re.DOTALL)
        if match:
            try:
                value = json.loads(match.group(0))
                return value if isinstance(value, dict) else None
            except json.JSONDecodeError:
                return None
    return None


def call_ollama(prompt: str, model: str, base_url: str, timeout: int) -> str:
    response = requests.post(
        f"{base_url.rstrip('/')}/api/generate",
        json={"model": model, "prompt": prompt, "stream": False, "options": {"temperature": 0.45}},
        timeout=timeout,
    )
    response.raise_for_status()
    text = response.json().get("response", "").strip()
    if not text:
        raise RuntimeError("Ollama returned an empty response.")
    return text


def build_prompt(scenario: dict[str, Any], query: str, context: list[str], object_name: str, object_description: str, history: list[dict[str, str]], coaching_mode: str) -> str:
    history_text = "\n".join(f"{item['role']}: {item['text']}" for item in history[-MAX_HISTORY:]) or "No previous turns."
    object_text = f"Name: {object_name}\nDescription: {object_description}" if object_name or object_description else "No selected object."
    context_text = "\n".join(f"- {item}" for item in context) or "No curated evidence was found."
    return f"""You are {scenario['role']} in the {scenario['title']} Unity scenario.
Style: {scenario['style']}.
Use supplied scenario evidence as the preferred source. Do not invent visual details.
Current object metadata:\n{object_text}
Curated scenario evidence:\n{context_text}
Recent local conversation:\n{history_text}
Social coaching mode: {coaching_mode or 'none'}
User: {query}

Return only JSON with response, emotion, action, intent, navigation_target.
Allowed emotions: {', '.join(sorted(ALLOWED_EMOTIONS))}.
Allowed actions: {', '.join(sorted(ALLOWED_ACTIONS))}.
Allowed intents: {', '.join(sorted(ALLOWED_INTENTS))}.
Keep response under 130 words. Use intent object_explanation and action point when selected-object metadata matters. For coaching, give one strength, one actionable improvement, and one next prompt."""


def offline_reply(scenario: str, query: str, object_name: str, context: list[str], coaching_mode: str) -> dict[str, Any]:
    title = SCENARIOS[scenario]["title"]
    if coaching_mode:
        return {"response": f"Practice mode is active for {coaching_mode}. You communicated a clear starting point. Try adding one concrete example and end with a confident summary. Would you like to try the next question?", "emotion": "friendly", "action": "encourage", "intent": "coach_feedback"}
    if object_name:
        evidence = context[0] if context else "Its metadata has been sent to the local assistant."
        return {"response": f"You are looking at {object_name}. {evidence}", "emotion": "friendly", "action": "point", "intent": "object_explanation"}
    return {"response": f"{title} is running in offline guidance mode because Ollama is not available. Start Ollama and pull the configured Llama model for generated answers. Your question was: {query}", "emotion": "thinking", "action": "nod", "intent": "answer"}


def create_app(test_config: dict[str, Any] | None = None) -> Flask:
    app = Flask(__name__)
    app.config.update(
        OLLAMA_URL=os.getenv("OLLAMA_BASE_URL", "http://127.0.0.1:11434"),
        OLLAMA_MODEL=os.getenv("OLLAMA_MODEL", "llama3.2:3b"),
        OLLAMA_TIMEOUT=int(os.getenv("OLLAMA_TIMEOUT_SECONDS", "45")),
    )
    if test_config:
        app.config.update(test_config)
    CORS(app, resources={r"/*": {"origins": "*"}})  # Unity standalone uses a local origin.
    store = LocalStore()

    @app.get("/health")
    def health():
        return jsonify({"status": "ok", "service": "embodied-ai-local", "model": app.config["OLLAMA_MODEL"], "scenarios": list(SCENARIOS)})

    @app.get("/scenarios")
    def scenarios():
        return jsonify({"scenarios": [{"id": key, **value} for key, value in SCENARIOS.items()]})

    @app.post("/sessions")
    def start_session():
        payload = request.get_json(silent=True) or {}
        scenario = str(payload.get("scenario", "academic_tutor"))
        if scenario not in SCENARIOS:
            return jsonify({"error": "Unknown scenario."}), 404
        session_id, _ = store.create(scenario)
        return jsonify({"session_id": session_id, "scenario": scenario}), 201

    @app.get("/sessions/<session_id>/history")
    def session_history(session_id: str):
        session = store.sessions.get(session_id)
        if not session:
            return jsonify({"error": "Session was not found or has expired."}), 404
        return jsonify({"session_id": session_id, "scenario": session.scenario, "history": session.history})

    @app.post("/coaching/start")
    def coaching_start():
        payload = request.get_json(silent=True) or {}
        mode = str(payload.get("mode", "interview")).lower().strip()
        if mode not in {"interview", "presentation", "conversation"}:
            return jsonify({"error": "Mode must be interview, presentation, or conversation."}), 400
        session_id, session = store.get_or_create(payload.get("session_id"), "social_coach")
        session.coaching_mode, session.coaching_turn = mode, 1
        prompts = {"interview": "Tell me about yourself and one achievement you are proud of.", "presentation": "Give a 30-second opening for a topic you know well.", "conversation": "Start a relaxed conversation with a new classmate."}
        return jsonify({"session_id": session_id, "mode": mode, "prompt": prompts[mode], "turn": session.coaching_turn})

    @app.post("/chat")
    def chat():
        payload = request.get_json(silent=True)
        if not isinstance(payload, dict):
            return jsonify({"error": "Request body must be valid JSON."}), 400
        query = str(payload.get("query", "")).strip()
        scenario = str(payload.get("scenario", "academic_tutor")).strip()
        if not query:
            return jsonify({"error": "Field 'query' is required."}), 400
        if scenario not in SCENARIOS:
            return jsonify({"error": f"Unknown scenario '{scenario}'."}), 404
        object_name = str(payload.get("object_name", "")).strip()[:120]
        object_description = str(payload.get("object_description", "")).strip()[:1000]
        session_id, session = store.get_or_create(payload.get("session_id"), scenario)
        context = read_knowledge(scenario, f"{query} {object_name} {object_description}")
        fallback_emotion = user_emotion(query)
        prompt = build_prompt(SCENARIOS[scenario], query, context, object_name, object_description, session.history, session.coaching_mode)
        try:
            parsed = extract_json(call_ollama(prompt, app.config["OLLAMA_MODEL"], app.config["OLLAMA_URL"], app.config["OLLAMA_TIMEOUT"]))
            result = normalise_result(parsed or {"response": "I could not format that answer."}, fallback_emotion)
        except (requests.RequestException, RuntimeError, ValueError) as error:
            result = normalise_result(offline_reply(scenario, query, object_name, context, session.coaching_mode), fallback_emotion, offline=True)
            result["diagnostic"] = str(error)[:240]
        session.history.extend([{"role": "user", "text": query}, {"role": "assistant", "text": result["response"]}])
        session.history = session.history[-MAX_HISTORY * 2:]
        result.update({"session_id": session_id, "scenario": scenario, "sources": context})
        return jsonify(result)

    return app


if __name__ == "__main__":
    create_app().run(host=os.getenv("FLASK_HOST", "127.0.0.1"), port=int(os.getenv("FLASK_PORT", "5000")), debug=os.getenv("FLASK_DEBUG") == "1")

"""Local character direction shared by curated dialogue and model prompts.

The identities describe fictional guides, not actual people. No model gets to
select a different speaker or send an in-world command through these fields.
"""
from __future__ import annotations

import re
from typing import Any


PERSONAS: dict[str, dict[str, str]] = {
    "general_assistant": {
        "id": "lumen", "name": "Lumen", "role": "Worlds companion",
        "style": "warm and curious; calm pace, short invitations, no sales language",
        "direction": "You are the local AI companion at the Worlds hub. Help visitors choose an experience, and keep ordinary conversation here unless they explicitly ask to move.",
        "greeting": "Hi! I'm Lumen, your local guide. Marcus explores Ancient Rome, Ada teaches science, Maya hosts the café, and Imani curates the museum. What would you like to discover?",
        "introduction": "I'm Lumen, your local AI companion. I can help you explore the worlds, meet their guides, or simply talk here with you.",
        "invitation": "We could explore Roman life with Marcus, try an experiment with Ada, practise a café conversation with Maya, or investigate an artifact with Imani. Which sounds interesting?",
    },
    "ancient_rome": {
        "id": "marcus", "name": "Marcus", "role": "Historian · Ancient Rome",
        "style": "observant and warmly scholarly; measured delivery, concrete details, one question at a time",
        "direction": "You are a fictional contemporary historian hosting a digital reconstruction of Ancient Rome, not an ancient Roman eyewitness. Distinguish surviving evidence from interpretation. Describe ordinary people as well as monuments, without inventing what is currently visible.",
        "greeting": "Salve, and welcome! I'm Marcus, your historian for this reconstruction of Ancient Rome. We can investigate the Forum, Roman engineering, or everyday life. Where shall we begin?",
        "introduction": "I'm Marcus, a virtual historian guiding this reconstruction of Ancient Rome. I help separate what the evidence tells us from the details we have to reconstruct.",
        "invitation": "Let's investigate the Forum as a place where people met, traded, and argued. Would you like to start with public life, Roman engineering, or how historians use evidence?",
    },
    "classroom": {
        "id": "ada", "name": "Ada", "role": "Science educator",
        "style": "patient and curious; clear short explanations, encouraging questions, no exaggerated praise",
        "direction": "You are a fictional science educator. Teach one idea at a time, invite predictions, and describe diagrams as simplified models. If the learner is confused, change the explanation rather than repeating it. Do not treat a short answer as proof of understanding.",
        "greeting": "Hello, I'm Ada! This is our science studio. Pick something you're curious about—an atom, a planet, or a living cell—and we can explore it together.",
        "introduction": "I'm Ada, your virtual science teacher. I use models, small questions, and everyday examples to help an idea make sense. It's always fine to ask me to slow down.",
        "invitation": "Pick a topic and we'll look at one model together. You can ask what it shows, how it works, or say 'quiz me' after we explore it. What are you curious about?",
    },
    "cafe": {
        "id": "maya", "name": "Maya", "role": "Barista · Conversation partner",
        "style": "easygoing and attentive; natural short replies, friendly pauses, never pressure a guest",
        "direction": "You are Maya, a fictional café barista and conversation-practice partner. Speak directly as the barista, not as a narrator describing her. Ask one helpful follow-up, preserve the requested drink and preferences, and never claim a payment or order has actually been placed. Actual simulated orders are handled by the Unity menu. Do not invent menu prices, allergy guarantees, stock, or order status.",
        "greeting": "Hi, I'm Maya—welcome in! We can practise an order, have a relaxed chat, or take a moment to settle in. What would you like today?",
        "introduction": "I'm Maya, the virtual barista here. This is a place to practise ordering and conversation at your own pace. The orders are simulated, so no money is charged.",
        "invitation": "Let's practise an order together. Start with the drink you'd like, and I'll ask one question at a time. You can restart or change your mind whenever you want.",
    },
    "museum": {
        "id": "imani", "name": "Imani", "role": "Museum curator",
        "style": "thoughtful and inviting; a clear observation before an interpretation, respectful of each culture",
        "direction": "You are a fictional museum curator. Explain artifacts in their cultural context, distinguish replicas from original objects, and distinguish evidence from uncertain attribution. Never claim the Unity replica is the original artifact or invent a provenance.",
        "greeting": "Welcome, I'm Imani. Every object here opens a question about the people who made or used it. Shall we begin with ancient writing, ingenious machines, or an object that catches your eye?",
        "introduction": "I'm Imani, your virtual curator. We'll explore these digital replicas by asking what we can observe, what the evidence supports, and what remains uncertain.",
        "invitation": "Choose an artifact and we'll look at its purpose, the people behind it, and the clues it preserves. The Rosetta Stone and Antikythera Mechanism are good places to begin.",
    },
}


HOSTS = {
    "kai": ("Kai", "Discovery companion", "friendly, relaxed and curious; conversational, brief and encouraging", "Help the visitor feel welcome. Pick up on their interests and feelings; suggest a small next step without pushing them."),
    "orion": ("Orion", "Curator and storyteller", "thoughtful, gently playful and observant; a light sense of wonder without flowery speeches", "Invite the visitor to notice details. Distinguish evidence, reconstruction and imagination. Never invent an artifact's provenance."),
    "theo": ("Professor Theo", "Science teacher", "patient, precise, dry gentle humor; one clear idea at a time", "Use everyday analogies and occasional short questions. Change your explanation when the learner is confused. Never claim understanding they have not demonstrated."),
    "aurelius": ("Aurelius", "Roman market guide", "sociable, practical and lively; concrete everyday details", "Portray a fictional Roman citizen in a historical reconstruction. Talk about people, markets and daily life while distinguishing evidence from role-play."),
    "lucius": ("Lucius", "Roman guard and guide", "calm, direct, courteous and protective; reserved warmth", "Portray a fictional Roman guard in a historical reconstruction. Explain civic life and the arena carefully. Do not pretend to be an actual ancient eyewitness."),
    "maya": ("Maya", "Cafe host and conversation partner", "easygoing, attentive and warm; natural short replies", "Be a welcoming fictional cafe host. Help with simulated orders and conversation practice; respond to what the visitor actually says."),
}


# One authored identity catalog, also loaded by the Unity wardrobe/labels.
import json
from pathlib import Path
_identity_path = Path(__file__).resolve().parent.parent / "Assets" / "Resources" / "LumenCharacterIdentities.json"
if not _identity_path.is_file():
    _identity_path = Path(__file__).resolve().parent / "data" / "characters.json"
IDENTITIES = {p["id"]: p for p in json.loads(_identity_path.read_text(encoding="utf-8-sig"))["characters"]}
HOSTS = {key: (p["name"],p["role"],p["manner"],p["direction"]) for key,p in IDENTITIES.items()}
LEGACY_IDENTITIES = {"lumen":"kai","ada":"theo","marcus":"aurelius","imani":"orion"}
for _scenario,_id in {"general_assistant":"kai","classroom":"theo","academic_tutor":"theo","ancient_rome":"aurelius","historical_guide":"aurelius","museum":"orion","cafe":"maya","social_coach":"maya"}.items():
    if _scenario not in PERSONAS: continue
    _p=IDENTITIES[_id]
    PERSONAS[_scenario].update(id=_id,name=_p["name"],role=_p["role"],style=_p["manner"],direction=_p["direction"],
        greeting=f"Hi! I'm {_p['name']}. What would you like to explore?",
        introduction=f"I'm {_p['name']}, your {_p['role'].lower()}. I'm a fictional virtual guide.",
        invitation="We can explore together at your pace. What catches your interest?")


def select_persona(scenario: str, speaker_id: str = "", object_name: str = "", object_description: str = "") -> dict[str, str]:
    """The active world owns the main speaker; recognized ambient people keep their role."""
    persona = dict(PERSONAS[scenario])
    host_id = speaker_id.lower().strip()
    host_id = LEGACY_IDENTITIES.get(host_id,host_id)
    if host_id in HOSTS:
        name, role, style, direction = HOSTS[host_id]
        persona.update(id=host_id, name=name, role=role, style=style,
            direction=direction + " Stay grounded in the active scenario and the conversation. You are a fictional virtual character, not a human.",
            greeting=f"Hi, I'm {name}. What caught your interest here?",
            introduction=f"I'm {name}, your {role.lower()}. What would you like to explore together?",
            invitation="We can look around together or talk about whatever caught your interest. Where would you like to start?")
        return persona
    selected = f"{object_name} {object_description}".lower()
    # The client may pass the main character ID even while inspecting a prop.
    if speaker_id == persona["id"]:
        return persona
    staff_description = re.search(r"(?i)^(?:a |the )?(?:fictional )?(?:cafe |café )?(?:barista|chef|staff member)\b|\bis (?:a |the )?(?:fictional )?(?:cafe |café )?(?:barista|chef|staff member)\b", object_description)
    if scenario == "cafe" and object_name and object_name.lower() != "maya" and (re.search(r"\b(barista|chef|staff)\b", object_name.lower()) or staff_description):
        persona.update(id="cafe_staff_"+re.sub(r"[^a-z0-9]+","_",object_name.lower()).strip("_")[:48],
            name=object_name[:80],role="Cafe staff",direction="Respond as the named fictional cafe staff member, never as Maya or the visitor's companion. Orders use the simulated menu; no real payments or invented order status.",
            greeting=f"Hello, I'm {object_name[:80]}, one of the cafe staff. You can browse the simulated menu whenever you like.",
            introduction=f"I'm {object_name[:80]}, a fictional cafe staff member. Maya is your companion.",
            invitation="Would you like to browse the simulated menu, or ask about the cafe?")
    elif scenario == "cafe" and object_name and re.search(r"\b(guest|customer|patron)\b", selected) and not re.search(r"\b(barista|chef|maya)\b", object_name.lower()):
        persona.update({
            "id": "cafe_guest_" + re.sub(r"[^a-z0-9]+", "_", object_name.lower()).strip("_")[:48],
            "name": object_name[:80], "role": "Café guest",
            "direction": "You are a fictional guest in a café conversation rehearsal, not the barista. Respond naturally to an introduction, ask one gentle question, and do not take orders or invent personal memories.",
            "greeting": "Hi, nice to meet you. Are you taking a break, or trying something new here today?",
            "introduction": f"I'm {object_name[:80]}, a virtual guest here for conversation practice. We can start with something simple, like how your day is going.",
            "invitation": "Let's start casually. What's one thing you've enjoyed today?",
        })
    elif scenario == "ancient_rome" and object_name and re.search(r"\b(resident|citizen|neighbor|neighbour|vendor|storyteller)\b", selected) and "marcus" not in object_name.lower():
        persona.update({
            "id": "roman_resident_" + re.sub(r"[^a-z0-9]+", "_", object_name.lower()).strip("_")[:48],
            "name": object_name[:80], "role": "Roman resident · Historical role-play",
            "direction": "You portray a fictional Roman resident in a learning reconstruction. Clearly acknowledge the role-play if asked; do not claim to be a real ancient eyewitness. Use the supplied historical evidence and admit uncertainty.",
            "greeting": "Salve! Welcome to the Forum. Are you here to explore the market, hear about daily life, or investigate the city's buildings?",
            "introduction": f"I'm {object_name[:80]}, a fictional resident in this Roman reconstruction. We can use this role-play to explore daily life, while Aurelius can help distinguish evidence from interpretation.",
            "invitation": "Would you like to talk about the market, public life in the Forum, or how water reached the city?",
        })
    return persona


def character_reply(query: str, persona: dict[str, str]) -> dict[str, Any] | None:
    text = re.sub(r"\s+", " ", query.lower()).strip(" .,!?'")
    answer = None
    if re.fullmatch(r"(?:hi|hello|hey|salve|good morning|good afternoon|good evening)(?: there)?", text) or re.search(r"\b(?:say hello|start a friendly conversation)\b", text):
        answer = persona["greeting"]
    elif re.fullmatch(r"(?:who are you|what are you|what(?:'s| is) your name|introduce yourself|tell me about yourself)(?: please)?", text):
        answer = persona["introduction"]
    elif re.fullmatch(r"(?:what can you do|how can you help(?: me)?|what should we do|where (?:do|should) we (?:start|begin)|let'?s (?:start|begin)|help me get started)", text):
        answer = persona["invitation"]
    elif re.fullmatch(r"(?:thanks|thank you|thx)(?: so much)?", text):
        answer = {"marcus": "You're welcome. What else would you like to investigate?", "ada": "You're welcome! Would you like another example, or are you ready to try a question?", "maya": "You're very welcome. Take your time—what would you like to do next?", "imani": "You're welcome. Shall we look closer at this object or explore another?"}.get(persona["id"], "You're welcome. What would you like to explore next?")
    if answer:
        return {"response": answer, "emotion": "friendly", "action": "nod" if text.startswith(("thanks", "thank you", "thx")) else "gesture", "intent": "conversation"}
    return None


def performance_tags(result: dict[str, Any], query: str) -> None:
    """Subtle, predictable acting directions; user distress outranks a happy model tag."""
    text = query.lower()
    if re.search(r"\b(?:i(?:'m| am| feel)|feeling)\s+(?:really\s+|very\s+|a little\s+)?(?:sad|upset|afraid|anxious|nervous|worried|lonely|overwhelmed)\b", text):
        result["emotion"] = "concerned"
        if result.get("action") not in {"show_model", "show_catalog"}:
            result["action"] = "listen"
    elif re.search(r"\b(?:i(?:'m| am) confused|i (?:do not|don't) understand|slow down)\b", text):
        result["emotion"] = "thinking"
        if result.get("action") not in {"show_model", "show_catalog"}:
            result["action"] = "explain"
    elif result.get("action") == "celebrate" and not re.search(r"\b(i\s+(?:did|finished|passed|won|completed|achieved|earned)|i\s+got\s+(?:the|a)|i'?m\s+proud|we\s+(?:won|finished|did\s+it))\b", text):
        result["action"] = "gesture"


# Short, grounded exercises. A question is offered only for a model the visitor
# has actually requested in this session, rather than guessing their view.
LEARNING_QUESTIONS = {
    "Atom": ("Our lithium-7 model has three protons. What does the number of protons determine: the element, or its electric charge?", r"\b(element|identity|atomic number)\b", "The number of protons determines the element. Three protons means lithium; changing electrons changes its charge."),
    "Mars": ("Mars has evidence of ancient flowing water. Does that, by itself, prove there was life?", r"\b(no|not|doesn'?t|cannot|can'?t)\b", "No. Evidence of water tells us about past conditions; it does not by itself establish that life existed."),
    "Plant Cell": ("Which part of a plant cell captures light for photosynthesis?", r"\bchloroplasts?\b", "Chloroplasts capture light for photosynthesis. The cell wall supports the cell, but it does not perform photosynthesis."),
    "Human Heart": ("How many chambers does a human heart have?", r"\b(four|4)\b", "The heart has four chambers: two atria and two ventricles."),
    "Water Molecule": ("A water molecule contains one oxygen atom. How many hydrogen atoms does it have?", r"\b(two|2)\b", "There are two hydrogen atoms and one oxygen atom in H₂O."),
    "Electric Circuit": ("For a simple lamp circuit to work, should its conducting path be open or closed?", r"\bclosed\b", "The conducting path must be closed so charge can flow through the circuit."),
    "Solar System": ("What object contains most of the Solar System's mass?", r"\bsun\b", "The Sun contains more than 99 percent of the Solar System's mass."),
    "DNA": ("What shape do the two DNA strands form?", r"\b(double helix|helix)\b", "They form a double helix. The paired bases carry hereditary information."),
}


def learning_reply(query: str, state: dict[str, str]) -> dict[str, Any] | None:
    text = query.lower().strip()
    if re.fullmatch(r"(?:quiz me|test me|ask me (?:a |one )?question|check my understanding)[.!?]*", text):
        topic = state.get("topic", "")
        if topic not in LEARNING_QUESTIONS:
            return {"response": "Let's first explore an atom, Mars, a plant cell, the heart, a water molecule, a circuit, the Solar System, or DNA. Then I can ask a question about that model.", "emotion": "friendly", "action": "encourage", "intent": "conversation"}
        state["quiz"] = topic
        return {"response": LEARNING_QUESTIONS[topic][0], "emotion": "thinking", "action": "listen", "intent": "conversation"}
    topic = state.get("quiz", "")
    if topic not in LEARNING_QUESTIONS:
        return None
    if re.fullmatch(r"(?:skip|stop|cancel|not now)[.!?]*", text):
        state.pop("quiz", None)
        return {"response": "Of course. We can explore at your pace. What would you like to look at next?", "emotion": "friendly", "action": "nod", "intent": "conversation"}
    # A new question or display request belongs to normal routing, not grading.
    if "?" in text or len(text.split()) > 14 or re.search(r"\b(show|explain|tell|what|why|how|where)\b", text):
        state.pop("quiz", None)
        return None
    state.pop("quiz", None)
    _, expected, explanation = LEARNING_QUESTIONS[topic]
    correct = bool(re.search(expected, text))
    # Do not pretend to have scored arbitrary reasoning or a negated answer.
    prefix = "That's the key idea. " if correct else "Let's check it together. "
    return {"response": prefix + explanation + " Would you like another example?", "emotion": "friendly", "action": "nod" if correct else "explain", "intent": "coach_feedback"}

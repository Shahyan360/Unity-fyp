"""Bounded optional rehearsal progression; no grading or raw-answer retention."""
import re

PROMPTS = {
    "interview": (
        "Tell me about yourself and one achievement you are proud of.",
        "Choose one example. Describe what you did and the result.",
        "Ask the interviewer one useful question, then close politely.",
    ),
    "presentation": (
        "Give a 30-second opening for a topic you know well.",
        "Add one concrete example that helps your audience understand.",
        "Give a concise closing sentence and invite a question.",
    ),
    "conversation": (
        "Start a relaxed conversation with a new classmate.",
        "Respond to their interest with one respectful question.",
        "Close the conversation politely, leaving room for them to decline.",
    ),
}


def current(session):
    mode = session.coaching_mode
    turn = session.coaching_turn
    prompt = PROMPTS[mode][max(0, min(2, turn - 1))] if mode in PROMPTS else ""
    return {"mode": mode, "turn": turn, "prompt": prompt, "active": bool(mode), "completed": False}


def start(session, mode):
    if mode not in PROMPTS:
        raise ValueError("Unknown practice mode")
    session.coaching_mode, session.coaching_turn = mode, 1
    session.coaching_receipts.clear()
    return current(session)


def end(session):
    session.coaching_mode, session.coaching_turn = "", 0
    session.coaching_receipts.clear()
    return {"active": False, "completed": False, "mode": "", "turn": 0,
            "prompt": "Practice ended. Explore or relax at your own pace."}


def advance(session, query, turn_id):
    if turn_id in session.coaching_receipts:
        return dict(session.coaching_receipts[turn_id])
    if not session.coaching_mode:
        raise ValueError("No practice session is active")
    text = query.strip()
    if re.search(r"(?i)\b(stop|cancel|quit|end|leave) (?:the |this |our )?(?:practice|exercise|session)\b|\b(?:don't|do not) want (?:to practise|to practice|practice|social practice)\b", text) or text.lower().strip(".! ") in {"stop", "cancel", "no thanks", "not now"}:
        result = end(session)
    elif re.match(r"(?i)^(?:what|where|how|why|when|show|take me|go |walk |repeat|can you|could you)\b", text):
        result = current(session)
        result["reason"] = "Exploration, clarification or repeat request; practice did not advance."
    else:
        result = current(session)
        answered = result["prompt"]
        if session.coaching_turn >= len(PROMPTS[session.coaching_mode]):
            mode = session.coaching_mode
            end(session)
            result = {"mode": mode, "turn": 3, "active": False, "completed": True,
                      "prompt": "Practice complete. You can restart, try another mode, or simply explore."}
        else:
            session.coaching_turn += 1
            result = current(session)
        result["answered_prompt"] = answered
    session.coaching_receipts[turn_id] = dict(result)
    while len(session.coaching_receipts) > 8:
        session.coaching_receipts.pop(next(iter(session.coaching_receipts)))
    return result

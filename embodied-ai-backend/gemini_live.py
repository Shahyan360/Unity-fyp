"""Local WebSocket bridge. Credentials never leave the Python process for Unity."""
import asyncio
import json
import os
import hashlib
from personas import select_persona


def api_key():
    key = os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY")
    if not key and os.name == "nt":
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as environment:
                key = winreg.QueryValueEx(environment, "GEMINI_API_KEY")[0]
        except OSError:
            pass
    return (key or "").strip()


def voices_for_person(speaker, object_name=""):
    voices={"kai":"Puck","orion":"Charon","theo":"Iapetus","aurelius":"Fenrir","lucius":"Orus","maya":"Aoede"}
    if not object_name:return voices.get(speaker,"Puck")
    # Stable across sessions/processes, independent of Python randomized hash.
    options=["Kore","Leda","Zephyr","Fenrir","Orus","Iapetus","Charon"]
    return options[int(hashlib.sha256((speaker+object_name).encode()).hexdigest()[:8],16)%len(options)]


def live_config(scenario, speaker, object_name="", object_description=""):
    persona = select_persona(scenario, speaker, object_name, object_description)
    if object_name:
        persona.update(name=object_name, role="local conversation partner", direction=f"You are {object_name}. Ground your fictional role in this supplied scene description: {object_description[:800]}. Respond as this person, never as the visitor helper. Baristas may handle simulated orders; guests do not.")
    elif scenario == "cafe":
        persona["direction"] += " You are the visitor companion and supportive friend, NOT the barista or shop employee. This cafe is a voluntary social rehearsal space. First ask whether they want relaxed exploration or social practice. Suggest one small step at a time: approach the counter, greet the barista, order, or introduce themselves to a guest. Respect refusals; never diagnose introversion or speak on the visitor behalf. The actual addressed barista or guest responds as themselves."
    voice = voices_for_person(speaker, object_name)
    voices = {"kai": "Puck", "orion": "Charon", "theo": "Iapetus", "aurelius": "Fenrir", "lucius": "Orus", "maya": "Aoede"}
    return {
        "response_modalities": ["AUDIO"],
        "input_audio_transcription": {}, "output_audio_transcription": {},
        "speech_config": {"voice_config": {"prebuilt_voice_config": {"voice_name": voice}}},
        "system_instruction": (
            f"You are {persona['name']}, {persona['role']}, a fictional virtual character in an interactive Unity experience. "
            f"Active scenario: {scenario}. {persona['direction']} Your manner: {persona['style']}. "
            "Speak directly and naturally, usually one or two short sentences. Listen to follow-ups and corrections, "
            "remember what the visitor actually said, and let them interrupt. Do not repeat introductions or canned praise. "
            "Respond when addressed or when speech relates to our conversation; ignore background conversations, TV, and your own speaker echo. "
            "Never invent visible buildings, people, exhibits, memories or completed actions. Ask when a destination is ambiguous. "
            "For take me or lead me requests pass the words take me to the destination to move_character so BOTH visitor and helper travel. "
            "In the classroom use show_model to actually display requested supported models before saying they are visible. Supported: Atom, Solar System, Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune, Plant Cell, Animal Cell, DNA, Human Heart, Water Cycle, Electric Circuit, Water Molecule, Volcano, Pythagorean Theorem, Plant, Animal Anatomy, Sunflower, Oak Tree, Light Spectrum, Periodic Table, Triceratops, Fern, Apollo 11. Never promise arbitrary models or live internet images. "
            "Use talk_to_person to address a named local person when requested. "
            "Use move_character for explicit movement commands, such as go there, go forward two meters, come here, follow me, "
            "stay here, stop, or go to a named nearby exhibit. For walking the visitor use move me forward two meters. "
            "Only say you are going somewhere after the tool confirms success. Never move for negated commands or discussions about movement. "
            "Use change_world only when the visitor explicitly asks to switch worlds. "
            "Keep historical evidence distinct from fiction. Museum replicas are not originals. Teach science with clear examples. "
            "Cafe orders are simulated, with no real payments. Be friendly without pretending to be a real human."
        ),
        "tools": [{"function_declarations": [
            {"name": "move_character", "description": "Execute an explicit visitor movement command in the current Unity scene. Safe path validation occurs in Unity.",
             "parameters": {"type": "OBJECT", "properties": {"command": {"type": "STRING"}}, "required": ["command"]}},
            {"name": "show_model", "description": "Actually display a supported classroom 3D model; only report success after confirmation.", "parameters": {"type": "OBJECT", "properties": {"command": {"type": "STRING"}}, "required": ["command"]}},
            {"name": "talk_to_person", "description": "Switch conversation from helper to an actual named nearby NPC.", "parameters": {"type": "OBJECT", "properties": {"command": {"type": "STRING"}}, "required": ["command"]}},
            {"name": "change_world", "description": "Switch worlds only at the visitor's explicit request.",
             "parameters": {"type": "OBJECT", "properties": {"scenario": {"type": "STRING", "enum": ["ancient_rome", "museum", "classroom", "cafe", "home"]}}, "required": ["scenario"]}}
        ]}],
    }


def register_live(app):
    from flask import jsonify, request
    from flask_sock import Sock
    from simple_websocket import ConnectionClosed
    sock = Sock(app)

    @app.get("/live/status")
    def live_status():
        return jsonify({"provider": "gemini-live", "configured": bool(api_key()),
                        "model": os.getenv("GEMINI_LIVE_MODEL", "gemini-3.8-live")})

    @sock.route("/live")
    def live_socket(ws):
        origin = request.headers.get("Origin", "")
        if origin and origin.split("/")[2].split(":")[0] not in {"localhost", "127.0.0.1", "[::1]"}:
            ws.close(); return
        key = api_key()
        def send(event, **values):
            ws.send(json.dumps({"event": event, **values}, ensure_ascii=False))
        if not key:
            send("error", text="Save GEMINI_API_KEY in Windows user environment variables.")
            return
        try:
            raw = ws.receive(timeout=10)
            setup = json.loads(raw) if isinstance(raw, str) else {}
            scenario = setup.get("scenario", "general_assistant")
            if scenario not in {"general_assistant", "ancient_rome", "museum", "classroom", "cafe"}:
                scenario = "general_assistant"
            speaker = str(setup.get("speaker_id", "kai"))[:40]
            asyncio.run(bridge(ws, send, key, scenario, speaker, str(setup.get("object_name", ""))[:100], str(setup.get("object_description", ""))[:1000]))
        except ConnectionClosed:
            pass
        except Exception as exc:
            # SDK exceptions can include request URLs. Never expose credentials.
            message = str(exc).replace(key, "[REDACTED]")[:500]
            try:
                send("error", text="Gemini Live: " + message)
            except ConnectionClosed:
                pass


async def bridge(ws, send, key, scenario, speaker, object_name="", object_description=""):
    from google import genai
    from google.genai import types
    from simple_websocket import ConnectionClosed
    client = genai.Client(api_key=key, http_options={"api_version": "v1beta"})
    try:
        async with client.aio.live.connect(model=os.getenv("GEMINI_LIVE_MODEL", "gemini-3.8-live"),
                                           config=live_config(scenario, speaker, object_name, object_description)) as session:
            send("ready", speaker=speaker, scenario=scenario)
            pending_calls = set()

            async def receive_unity():
                while True:
                    packet = await asyncio.to_thread(ws.receive, timeout=.5)
                    if packet is None:
                        continue
                    if isinstance(packet, bytes):
                        if len(packet) > 64000 or len(packet) % 2:
                            raise ValueError("Send short mono PCM16 microphone frames at 16000Hz.")
                        await session.send_realtime_input(audio=types.Blob(data=packet, mime_type="audio/pcm;rate=16000"))
                    else:
                        value = json.loads(packet)
                        if value.get("event") == "text":
                            text = str(value.get("text", ""))[:2000]
                            if text.strip():
                                await session.send_client_content(turns={"role": "user", "parts": [{"text": text}]}, turn_complete=True)
                        elif value.get("event") == "audio_end":
                            await session.send_realtime_input(audio_stream_end=True)
                        elif value.get("event") == "tool_result" and value.get("id") in pending_calls:
                            pending_calls.discard(value["id"])
                            await session.send_tool_response(function_responses=types.FunctionResponse(
                                id=value["id"], name=value["name"], response={"ok": bool(value.get("ok")), "result": str(value.get("text", ""))[:500]}))

            async def receive_gemini():
                spoken = ""
                heard = ""
                while True:
                    async for message in session.receive():
                        content = message.server_content
                        if content:
                            if content.interrupted:
                                send("interrupted")
                                spoken = ""
                            if content.input_transcription and content.input_transcription.text:
                                heard += content.input_transcription.text
                                send("input_text", text=heard)
                            if content.output_transcription and content.output_transcription.text:
                                spoken += content.output_transcription.text
                                send("output_text", text=spoken)
                            if content.model_turn:
                                for part in content.model_turn.parts or []:
                                    if part.inline_data and part.inline_data.data:
                                        ws.send(part.inline_data.data)
                            if content.turn_complete:
                                send("turn_complete", text=spoken)
                                spoken = ""
                                heard = ""
                        if message.tool_call:
                            for call in message.tool_call.function_calls:
                                if call.name not in {"move_character", "change_world", "show_model", "talk_to_person"}:
                                    continue
                                pending_calls.add(call.id)
                                send("command", id=call.id, name=call.name,
                                     command=str(call.args.get("command", "")), scenario=str(call.args.get("scenario", "")))
                        if message.tool_call_cancellation:
                            for identifier in message.tool_call_cancellation.ids:
                                pending_calls.discard(identifier)
                            send("cancel_commands")
                        if message.go_away:
                            send("reconnect", text="Refreshing the live conversation connection.")
                            return

            incoming = asyncio.create_task(receive_unity())
            outgoing = asyncio.create_task(receive_gemini())
            done, pending = await asyncio.wait({incoming, outgoing}, return_when=asyncio.FIRST_COMPLETED)
            for task in pending:
                task.cancel()
            await asyncio.gather(*pending, return_exceptions=True)
            for task in done:
                try:
                    task.result()
                except ConnectionClosed:
                    pass
    finally:
        await client.aio.aclose()

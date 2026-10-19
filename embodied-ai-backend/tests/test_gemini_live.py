from gemini_live import live_config, voices_for_person


def test_actual_display_and_partner_tools_are_available():
    config = live_config("classroom", "theo")
    names = {f["name"] for f in config["tools"][0]["function_declarations"]}
    assert names == {"show_model", "show_image", "move_character", "change_world", "talk_to_person"}
    assert "before saying they are visible" in config["system_instruction"]
    assert "Atom" in config["system_instruction"]


def test_cafe_helper_is_not_the_barista():
    text = live_config("cafe", "maya")["system_instruction"]
    assert "NOT the barista" in text
    assert "Respect refusals" in text


def test_guest_identity_and_voice_are_separate_from_helper():
    config = live_config("cafe", "guest_1", "Alex", "A quiet cafe guest reading a book.")
    assert "You are Alex" in config["system_instruction"]
    assert "never as the visitor helper" in config["system_instruction"]
    assert voices_for_person("guest_1", "Alex") == voices_for_person("guest_1", "Alex")
    assert voices_for_person("guest_1", "Alex") != voices_for_person("maya")


def test_provider_rejected_optional_flags_stay_absent():
    config = live_config("general_assistant", "kai")
    assert "proactivity" not in config
    assert "enable_affective_dialog" not in config


from gemini_live import LiveTurn, allowed_origin, NPC_VOICES


def test_tool_boundary_does_not_finish_reply_before_tool_response():
    turn = LiveTurn()
    turn.pending.add("display-1")
    assert turn.finish() is None
    turn.pending.remove("display-1")
    assert turn.finish() is None
    turn.spoken = "Here is the atom model."
    turn.has_audio = True
    assert turn.finish() == "Here is the atom model."
    assert turn.finish() is None


def test_parallel_tools_must_all_finish_before_final_reply():
    turn = LiveTurn()
    turn.pending.update(["one", "two"])
    turn.spoken = "Let me show you."
    turn.pending.remove("one")
    assert turn.finish() is None
    turn.pending.remove("two")
    assert turn.finish() == "Let me show you."


def test_interruption_discards_previous_transcription():
    turn = LiveTurn()
    turn.spoken = "An unfinished answer"
    turn.heard = "Old question"
    turn.has_audio = True
    turn.interrupt()
    assert turn.finish() is None
    turn.heard = "New question"
    turn.spoken = "New answer"
    assert turn.finish() == "New answer"


def test_native_and_loopback_origins_only():
    for origin in ("", "http://localhost:5000", "http://127.0.0.1:5000", "http://[::1]:5000"):
        assert allowed_origin(origin)
    for origin in ("null", "file:///tmp/example", "http://localhost.evil.test", "https://evil.test", "http://[broken"):
        assert not allowed_origin(origin)


def test_explicit_npc_voice_is_allowlisted_and_does_not_override_helper():
    assert voices_for_person("guest", "Alex", "Leda") == "Leda"
    assert voices_for_person("guest", "Alex", "untrusted") in NPC_VOICES
    assert voices_for_person("kai", "", "Leda") == "Puck"
    assert len(NPC_VOICES) == len(set(NPC_VOICES))

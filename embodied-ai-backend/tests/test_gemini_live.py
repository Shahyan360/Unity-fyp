from gemini_live import live_config, voices_for_person


def test_actual_display_and_partner_tools_are_available():
    config = live_config("classroom", "theo")
    names = {f["name"] for f in config["tools"][0]["function_declarations"]}
    assert names == {"show_model", "move_character", "change_world", "talk_to_person"}
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

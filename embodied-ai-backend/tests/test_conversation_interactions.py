from app import create_app, detect_requested_scenario


def test_intro_explains_lumen_capabilities():
    client = create_app({"TESTING": True}).test_client()
    result = client.post("/chat", json={"query": "How can you help me?", "scenario": "classroom"}).get_json()
    assert result["response_source"] == "local-introduction"
    assert all(word in result["response"].lower() for word in ("pictures", "science", "history", "press e"))


def test_gaze_identification_uses_supplied_context():
    client = create_app({"TESTING": True}).test_client()
    for question in ("What is this?", "What am I looking at?", "Tell me about that"):
        result = client.post("/chat", json={"query": question, "scenario": "classroom",
            "object_name": "Atom model", "object_description": "A nucleus surrounded by electrons."}).get_json()
        assert result["response_source"] == "local-object"
        assert "Atom model" in result["response"]
        assert "electrons" in result["response"]


def test_hub_questions_route_to_the_relevant_world():
    client = create_app({"TESTING": True}).test_client()
    for question, world in (("What is an atom?", "classroom"), ("Tell me about ancient Rome", "ancient_rome"),
                            ("I want to practise ordering coffee", "cafe"), ("Show me museum artifacts", "museum")):
        result = client.post("/chat", json={"query": question, "scenario": "general_assistant"}).get_json()
        assert result["next_scenario"] == world


def test_natural_navigation_and_negation():
    assert detect_requested_scenario("Could you navigate to the science studio?") == "classroom"
    assert detect_requested_scenario("Please travel to the museum") == "museum"
    assert detect_requested_scenario("Do not travel to the museum") == ""


def test_neural_voice_has_bounded_inputs_and_reports_readiness():
    client = create_app({"TESTING": True}).test_client()
    assert client.get("/speech/config").get_json() == {"engine": "piper", "ready": False}
    for payload in ({}, {"text": ""}, {"text": "x" * 2001}, {"text": 42}):
        assert client.post("/speech/synthesize", json=payload).status_code == 400
    assert client.post("/speech/synthesize", json={"text": "Hello"}).status_code == 503


def test_question_routes_across_worlds_and_honours_stay_here():
    client = create_app({"TESTING": True}).test_client()
    result = client.post("/chat", json={"query": "What is an atom?", "scenario": "cafe"}).get_json()
    assert result["next_scenario"] == "classroom"
    result = client.post("/chat", json={"query": "Stay here, show me an atom model", "scenario": "cafe"}).get_json()
    assert result["next_scenario"] == ""


def test_museum_visitor_keeps_own_identity():
    client = create_app({"TESTING": True}).test_client()
    result = client.post("/chat", json={"query": "Who are you?", "scenario": "museum",
        "object_name": "Museum visitor 5", "object_description": "A visitor exploring the gallery."}).get_json()
    assert result["speaker_name"] == "Museum visitor 5"
    assert result["speaker_id"].startswith("museum_visitor_")
    assert "virtual visitor" in result["response"]


def test_explicit_person_category_preserves_arbitrary_character_identity():
    client = create_app({"TESTING": True}).test_client()
    for scenario, name in (("classroom", "Alex"), ("ancient_rome", "Merchant Publius-23")):
        result = client.post("/chat", json={"query": "Who are you?", "scenario": scenario,
            "object_name": name, "object_description": "An interactive fictional character.",
            "object_category": "Person"}).get_json()
        assert result["speaker_name"] == name
        assert name in result["response"]


def test_gaze_contractions_and_empty_focus_are_grounded():
    client = create_app({"TESTING": True}).test_client()
    for question in ("whats this", "what’s that?", "What is it?"):
        result = client.post("/chat", json={"query": question, "scenario": "classroom",
            "object_name": "Earth", "object_description": "The third planet from the Sun."}).get_json()
        assert result["response_source"] == "local-object"
        assert "Earth" in result["response"]
    result = client.post("/chat", json={"query": "What am I looking at?", "scenario": "ancient_rome"}).get_json()
    assert result["response_source"] == "local-object"
    assert "don’t have an object in view" in result["response"]


def test_guidance_and_uncertain_provenance_answer_the_actual_question():
    client = create_app({"TESTING": True}).test_client()
    guidance = client.post("/chat", json={"query": "How do I get started?", "scenario": "cafe"}).get_json()
    assert guidance["response_source"] == "local-guidance"
    assert all(word in guidance["response"] for word in ("Menu", "Talk", "Worlds"))
    provenance = client.post("/chat", json={"query": "Who carved the Rosetta Stone?", "scenario": "museum"}).get_json()
    assert "can’t confirm" in provenance["response"] and "196 BCE" in provenance["response"]
    assert not provenance["next_scenario"]


def test_web_origin_requires_the_configured_host():
    client = create_app({"TESTING": True, "WEB_ORIGIN": "https://lumen.example"}).test_client()
    payload = {"query": "What should I do here?", "scenario": "museum"}
    assert client.post("/chat", json=payload, headers={"Origin": "https://lumen.example"}).status_code == 200
    assert client.post("/chat", json=payload, headers={"Origin": "https://other.example"}).status_code == 403

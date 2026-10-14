from app import call_ollama, create_app, extract_json


def test_health_and_scenarios_are_available():
    client = create_app({"TESTING": True}).test_client()
    assert client.get("/health").status_code == 200
    data = client.get("/scenarios").get_json()
    assert any(item["id"] == "classroom" for item in data["scenarios"])
    assert any(item["id"] == "ancient_rome" for item in data["scenarios"])
    assert any(item["id"] == "cafe" for item in data["scenarios"])
    assert any(item["id"] == "museum" for item in data["scenarios"])


def test_chat_creates_private_session_and_returns_unity_contract():
    client = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()
    response = client.post("/chat", json={"query": "What is this?", "scenario": "classroom", "object_name": "Atom Model"})
    assert response.status_code == 200
    data = response.get_json()
    assert data["response"] and data["session_id"]
    assert data["action"] in {"idle", "nod", "gesture", "point", "explain", "encourage", "listen", "celebrate", "show_model", "show_catalog"}
    assert data["offline"] is True
    history = client.get(f"/sessions/{data['session_id']}/history")
    assert history.status_code == 200
    assert len(history.get_json()["history"]) == 2


def test_social_coaching_starts_with_a_real_prompt():
    client = create_app({"TESTING": True}).test_client()
    response = client.post("/coaching/start", json={"mode": "interview"})
    assert response.status_code == 200
    assert "achievement" in response.get_json()["prompt"].lower()


def test_explicit_request_switches_to_authored_world_and_mentions_do_not():
    client = create_app({"TESTING": True}).test_client()
    for query, expected in (("Take me to Ancient Rome", "ancient_rome"), ("Open the classroom", "classroom"), ("Let's go to the café", "cafe"), ("Take me to the museum", "museum")):
        go = client.post("/chat", json={"query": query, "scenario": "general_assistant"}).get_json()
        assert go["next_scenario"] == expected
        assert go["intent"] == "change_scenario"

    app = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1})
    discussing = app.test_client().post("/chat", json={"query": "Tell me about Rome", "scenario": "general_assistant"}).get_json()
    assert discussing["next_scenario"] == ""


def test_show_atom_intent_works_without_ollama():
    client = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()
    data = client.post("/chat", json={"query": "Show me the atom model", "scenario": "classroom"}).get_json()
    assert data["action"] == "show_model"
    assert data["navigation_target"] == "Atom"
    assert "three protons and four neutrons" in data["response"]
    assert "quantum probability" in data["response"]


def test_learning_model_requests_return_curated_explanations(monkeypatch):
    def fail_if_called(*args):
        raise AssertionError("A curated exhibit should not wait for Llama.")
    monkeypatch.setattr("app.call_ollama", fail_if_called)
    client = create_app({"TESTING": True}).test_client()
    for query, target, expected in (
        ("Show me the solar system", "Solar System", "more than 99 percent"),
        ("Show a fern", "Fern", "reproduce with spores"),
        ("Show the plant cell", "Plant Cell", "photosynthesis"),
        ("Tell me about Mars", "Mars", "Olympus Mons"),
        ("Show me the periodic table", "Periodic Table", "atomic number"),
        ("What is a human heart?", "Human Heart", "four chambers"),
        ("Show me an animal cell", "Animal Cell", "mitochondria"),
        ("Show the DNA model", "DNA", "double helix"),
        ("Explain the water cycle", "Water Cycle", "evaporation"),
        ("Show a simple electric circuit", "Electric Circuit", "closed conducting path"),
        ("Show a water molecule", "Water Molecule", "two hydrogen atoms"),
        ("Show me a volcano", "Volcano", "magma"),
        ("Explain the Pythagorean theorem", "Pythagorean Theorem", "a² + b² = c²"),
        ("Show the visible light spectrum", "Light Spectrum", "red to violet"),
    ):
        data = client.post("/chat", json={"query": query, "scenario": "classroom"}).get_json()
        assert data["action"] == "show_model"
        assert data["navigation_target"] == target
        assert expected in data["response"]


def test_classroom_discovery_returns_full_catalog_without_ollama(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Classroom discovery must be immediate.")))
    client = create_app({"TESTING": True}).test_client()
    for query in ("What can you show me?", "What models do you have?", "List all the classroom exhibits"):
        data = client.post("/chat", json={"query": query, "scenario": "classroom"}).get_json()
        answer = data["response"].lower()
        assert data["action"] == "show_catalog"
        assert data["offline"] is False
        for item in ("solar system", "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune", "atom", "periodic table", "water molecule", "plant cell", "animal cell", "dna", "heart", "animal anatomy", "sunflower", "oak tree", "water cycle", "electric circuit", "volcano", "light spectrum", "pythagorean theorem"):
            assert item in answer


def test_specific_classroom_model_request_is_not_misclassified_as_catalog(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Specific model requests use curated exhibit data.")))
    data = create_app({"TESTING": True}).test_client().post("/chat", json={"query": "What can you show me about Mars?", "scenario": "classroom"}).get_json()
    assert data["action"] == "show_model"
    assert data["navigation_target"] == "Mars"


def test_planets_are_individual_exhibits_and_knowledge_is_immediate(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Must use local exhibit data.")))
    client = create_app({"TESTING": True}).test_client()
    for planet in ("Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune"):
        data = client.post("/chat", json={"query": f"Tell me about {planet}", "scenario": "classroom"}).get_json()
        assert data["navigation_target"] == planet
        assert data["action"] == "show_model"


def test_home_exhibit_requests_route_to_the_correct_world_and_keep_the_target(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Exhibit routing must be immediate.")))
    client = create_app({"TESTING": True}).test_client()
    for query, destination, target, detail in (
        ("Show me Earth in detail", "classroom", "Earth", "third planet"),
        ("Show me a plant", "classroom", "Plant", "roots"),
        ("Show me an animal", "classroom", "Animal Anatomy", "skeleton"),
        ("Show me a sunflower", "classroom", "Sunflower", "florets"),
        ("Show me an oak tree", "classroom", "Oak Tree", "acorns"),
        ("Show the Rosetta Stone", "museum", "Rosetta Stone", "hieroglyphs"),
    ):
        data = client.post("/chat", json={"query": query, "scenario": "general_assistant"}).get_json()
        assert data["next_scenario"] == destination
        assert data["action"] == "show_model"
        assert data["navigation_target"] == target
        assert detail in data["response"].lower()


def test_museum_artifact_explanations_are_curated_and_fast(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Museum artifact facts are curated.")))
    client = create_app({"TESTING": True}).test_client()
    for query, target in (("Tell me about the Rosetta Stone", "Rosetta Stone"), ("Show the Antikythera Mechanism", "Antikythera Mechanism"), ("Show the Roman coin", "Roman Coin")):
        data = client.post("/chat", json={"query": query, "scenario": "museum"}).get_json()
        assert data["navigation_target"] == target
        assert data["action"] == "show_model"


def test_greetings_reply_without_waiting_for_local_model(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Small talk should be instant.")))
    data = create_app({"TESTING": True}).test_client().post("/chat", json={"query": "Hello!", "scenario": "general_assistant"}).get_json()
    assert data["response"].startswith("Hi!")
    assert data["offline"] is False


def test_greeting_with_how_are_you_is_fast_without_ollama(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("A normal greeting should not wait for generation.")))
    client = create_app({"TESTING": True}).test_client()
    for query in ("Hi, how are you today?", "How are you doing today?"):
        data = client.post("/chat", json={"query": query, "scenario": "general_assistant"}).get_json()
        assert "doing well" in data["response"].lower()
        assert data["offline"] is False


def test_root_route_explains_local_api_instead_of_returning_not_found():
    response = create_app({"TESTING": True}).test_client().get("/")
    assert response.status_code == 200
    assert response.get_json()["health"] == "/health"


def test_everyday_assistance_returns_immediately_without_ollama():
    client = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()
    response = client.post("/chat", json={"query": "What is one practical way to plan my day?", "scenario": "general_assistant"})
    assert response.status_code == 200
    data = response.get_json()
    assert "three outcomes" in data["response"]
    assert data["offline"] is False
    assert data["intent"] == "conversation"


def test_common_day_to_day_and_cafe_prompts_are_immediate(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Common conversation should not wait for generation.")))
    client = create_app({"TESTING": True}).test_client()
    day = client.post("/chat", json={"query": "My day was odd; ask me a friendly question to help me talk it through.", "scenario": "general_assistant"}).get_json()
    assert "what felt most different" in day["response"].lower()
    cafe = client.post("/chat", json={"query": "Help me order a cappuccino politely.", "scenario": "cafe"}).get_json()
    assert "cappuccino" in cafe["response"].lower()
    assert "simulated order" in cafe["response"].lower()
    assert cafe["intent"] == "conversation"
    starter = client.post("/chat", json={"query": "How can I start a conversation with someone new?", "scenario": "general_assistant"}).get_json()
    assert "low-pressure opener" in starter["response"].lower()
    assert starter["offline"] is False


def test_roman_people_greet_the_visitor_without_waiting_for_ollama(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("A Roman greeting should be immediate.")))
    data = create_app({"TESTING": True}).test_client().post("/chat", json={
        "query": "Say hello and start a friendly conversation with Roman neighbor.",
        "scenario": "ancient_rome",
        "object_name": "Roman neighbor",
        "object_description": "A resident visiting the market in this Ancient Rome learning world.",
    }).get_json()
    assert data["intent"] == "conversation"
    assert data["offline"] is False
    assert "salve" in data["response"].lower()
    assert "market" in data["response"].lower()


def test_cafe_order_keeps_the_drink_the_user_requested_and_coaches_social_anxiety(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("Routine café help should be instant.")))
    client = create_app({"TESTING": True}).test_client()
    espresso = client.post("/chat", json={"query": "Can I order an espresso with oat milk?", "scenario": "cafe"}).get_json()
    assert "espresso" in espresso["response"].lower()
    assert "cappuccino" not in espresso["response"].lower()
    assert "oat milk" in espresso["response"].lower()
    coaching = client.post("/chat", json={"query": "I feel nervous about introducing myself", "scenario": "social_coach"}).get_json()
    assert "first step small" in coaching["response"].lower()
    assert "introduce" not in coaching["response"].lower() or "say" in coaching["response"].lower()


def test_common_gravity_question_is_answered_from_local_knowledge_immediately(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: (_ for _ in ()).throw(AssertionError("A basic stored fact should not wait for a full generation.")))
    client = create_app({"TESTING": True}).test_client()
    data = client.post("/chat", json={"query": "Explain gravity simply", "scenario": "academic_tutor"}).get_json()
    assert "9.8 m/s" in data["response"]
    assert data["scenario"] == "classroom"
    assert data["offline"] is False


def test_animal_chat_is_not_misrouted_to_a_classroom_model(monkeypatch):
    calls = []
    payload = '{"response":"Cats may purr for comfort or communication.","emotion":"curious","action":"explain","intent":"answer","navigation_target":"","next_scenario":""}'
    monkeypatch.setattr("app.call_ollama", lambda *args: calls.append(args) or payload)
    client = create_app({"TESTING": True}).test_client()
    data = client.post("/chat", json={"query": "Why do cats purr?", "scenario": "general_assistant"}).get_json()
    assert calls
    assert data["next_scenario"] == ""
    assert data["navigation_target"] == ""
    assert "purr" in data["response"].lower()


def test_local_generation_uses_a_concise_budget(monkeypatch):
    captured = {}

    class Response:
        def raise_for_status(self):
            pass

        def json(self):
            return {"response": "{}"}

    def post(url, json, timeout):
        captured.update(json)
        return Response()

    monkeypatch.setattr("app.requests.post", post)
    assert call_ollama("prompt", "llama3.2:1b", "http://127.0.0.1:11434", 30) == "{}"
    assert captured["options"]["num_predict"] == 104
    assert captured["options"]["num_ctx"] == 1536


def test_truncated_model_json_keeps_a_complete_spoken_answer():
    data = extract_json('{"response":"Weather changes day to day; climate is the long-term pattern.","emotion":"friendly","action":"explain",')
    assert data is not None
    assert "long-term pattern" in data["response"]


def test_celebration_gesture_is_reserved_for_clear_achievements(monkeypatch):
    payload = '{"response":"That is fantastic!","emotion":"excited","action":"celebrate","intent":"conversation","navigation_target":"","next_scenario":""}'
    monkeypatch.setattr("app.call_ollama", lambda *args: payload)
    client = create_app({"TESTING": True}).test_client()
    ordinary = client.post("/chat", json={"query": "What does florp mean?", "scenario": "general_assistant"}).get_json()
    assert ordinary["action"] == "gesture"
    achievement = client.post("/chat", json={"query": "I passed my exam!", "scenario": "general_assistant"}).get_json()
    assert achievement["action"] == "celebrate"


def test_legacy_scenario_names_are_accepted_as_aliases():
    client = create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()
    data = client.post("/chat", json={"query": "What is a Roman forum?", "scenario": "historical_guide"}).get_json()
    assert data["scenario"] == "ancient_rome"


def test_malformed_model_json_uses_safe_offline_fallback(monkeypatch):
    monkeypatch.setattr("app.call_ollama", lambda *args: '{"emotion":"friendly"}')
    client = create_app({"TESTING": True}).test_client()
    data = client.post("/chat", json={"query": "Explain one idea from these notes", "scenario": "ancient_rome"}).get_json()
    assert data["offline"] is True
    assert "usable answer" in data["response"]
    assert data["diagnostic"]


def test_model_cannot_trigger_an_unrequested_scene_change(monkeypatch):
    payload = '{"response":"The Forum was Rome’s civic center.","emotion":"friendly","action":"gesture","intent":"change_scenario","navigation_target":"","next_scenario":"cafe"}'
    monkeypatch.setattr("app.call_ollama", lambda *args: payload)
    client = create_app({"TESTING": True}).test_client()
    data = client.post("/chat", json={"query": "Tell me about the Roman Forum", "scenario": "ancient_rome"}).get_json()
    assert data["next_scenario"] == ""
    assert data["intent"] == "answer"

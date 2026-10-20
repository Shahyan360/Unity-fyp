import pytest
from app import create_app


@pytest.fixture
def client():
    return create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()


@pytest.mark.parametrize("scenario,name", [("classroom", "Professor Theo"), ("cafe", "Maya"), ("ancient_rome", "Aurelius"), ("museum", "Orion")])
def test_world_owns_character_identity(client, scenario, name):
    data = client.post('/chat', json={"query": "Who are you?", "scenario": scenario}).get_json()
    assert data['speaker_name'] == name
    assert name in data['response']
    assert not data['next_scenario']


@pytest.mark.parametrize("query", ["Tell me about Mars", "I read about the Rosetta Stone", "Don't show me Mars", "I don't want to go to the museum"])
def test_mentions_and_negations_do_not_move_visitor(client, query):
    data = client.post('/chat', json={"query": query, "scenario": "cafe"}).get_json()
    assert not data['next_scenario']


def test_classroom_exercise_remembers_requested_topic(client):
    display = client.post('/chat', json={"query": "Show an atom", "scenario": "classroom"}).get_json()
    session = display['session_id']
    quiz = client.post('/chat', json={"query": "quiz me", "scenario": "classroom", "session_id": session}).get_json()
    assert "protons" in quiz['response']
    answer = client.post('/chat', json={"query": "the element", "scenario": "classroom", "session_id": session}).get_json()
    assert answer['intent'] == 'coach_feedback'
    assert "lithium" in answer['response'].lower()


def test_distress_has_appropriate_acting_direction(client):
    data = client.post('/chat', json={"query": "I feel nervous", "scenario": "cafe"}).get_json()
    assert data['emotion'] == 'concerned'
    assert data['action'] == 'listen'


def test_ambient_cafe_guest_is_not_the_barista(client):
    data = client.post('/chat', json={"query": "hello", "scenario": "cafe", "object_name": "Cafe guest Alex"}).get_json()
    assert data['speaker_name'] == 'Cafe guest Alex'
    assert data['speaker_id'] != 'maya'


def test_guest_description_denying_staff_role_does_not_make_guest_staff(client):
    data = client.post('/chat', json={"query":"Who are you?","scenario":"cafe","object_name":"Alex","object_description":"A fictional cafe guest reading a book. You are not the barista or visitor helper."}).get_json()
    assert data['speaker_name'] == 'Alex'
    assert data['speaker_id'].startswith('cafe_guest_')


def test_named_staff_remain_distinct_from_maya_companion(client):
    data = client.post('/chat', json={"query":"Who are you?","scenario":"cafe","object_name":"Cafe barista","object_description":"A fictional cafe staff member."}).get_json()
    assert data['speaker_name'] == 'Cafe barista'
    assert data['speaker_id'].startswith('cafe_staff_')
    assert 'Maya' in data['response']


@pytest.mark.parametrize('old,new', [('ada','theo'),('marcus','aurelius'),('imani','orion'),('lumen','kai')])
def test_legacy_speaker_ids_resolve_to_current_identity(client,old,new):
    data = client.post('/chat',json={"query":"Who are you?","scenario":"general_assistant","speaker_id":old}).get_json()
    assert data['speaker_id'] == new

import pytest
from app import create_app


@pytest.fixture
def client():
    return create_app({"TESTING": True, "OLLAMA_URL": "http://127.0.0.1:1", "OLLAMA_TIMEOUT": 1}).test_client()


@pytest.mark.parametrize("scenario,name", [("classroom", "Ada"), ("cafe", "Maya"), ("ancient_rome", "Marcus"), ("museum", "Imani")])
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

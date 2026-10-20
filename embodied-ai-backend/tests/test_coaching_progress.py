import pytest
from app import create_app


@pytest.mark.parametrize("mode", ["conversation", "interview", "presentation"])
def test_actual_practice_progress_is_bounded_and_duplicate_turns_do_not_advance(mode):
    client = create_app({"TESTING": True}).test_client()
    started = client.post("/coaching/start", json={"mode": mode}).get_json()
    identifier = started["session_id"]
    assert started["active"] and started["turn"] == 1
    payload = {"session_id": identifier, "query": "Hello, I enjoy astronomy.", "turn_id": "turn-one"}
    result = client.post("/coaching/turn", json=payload).get_json()
    assert result["turn"] == 2 and result["answered_prompt"] == started["prompt"]
    assert client.post("/coaching/turn", json=payload).get_json() == result
    for index in (2, 3):
        payload.update(query="I will use a specific example and listen carefully.", turn_id=f"turn-{index}")
        result = client.post("/coaching/turn", json=payload).get_json()
    assert result["completed"] and not result["active"] and result["turn"] == 3
    assert client.post("/coaching/turn", json=payload).get_json() == result


def test_clarification_does_not_count_as_practice_and_opt_out_ends_it():
    client = create_app({"TESTING": True}).test_client()
    identifier = client.post("/coaching/start", json={"mode": "conversation"}).get_json()["session_id"]
    payload = {"session_id": identifier, "query": "Can you repeat the question?", "turn_id": "repeat"}
    assert client.post("/coaching/turn", json=payload).get_json()["turn"] == 1
    payload.update(query="I do not want social practice.", turn_id="decline")
    assert not client.post("/coaching/turn", json=payload).get_json()["active"]
    payload.update(query="Hello", turn_id="after-end")
    assert client.post("/coaching/turn", json=payload).status_code == 409


def test_end_rejects_old_turns_instead_of_reactivating_cached_progress():
    client=create_app({"TESTING":True}).test_client()
    identifier=client.post('/coaching/start',json={"mode":"conversation"}).get_json()['session_id']
    payload={"session_id":identifier,"query":"Hello, I like astronomy.","turn_id":"old-turn"}
    assert client.post('/coaching/turn',json=payload).get_json()['active']
    client.post('/coaching/end',json={"session_id":identifier})
    assert client.post('/coaching/turn',json=payload).status_code==409

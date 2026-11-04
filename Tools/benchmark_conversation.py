"""Measure actual Flask streaming, sentence latency and local answer provenance."""
import argparse
import json
import time
from pathlib import Path
import requests

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    parser.add_argument('--url', default='http://127.0.0.1:5000')
    args = parser.parse_args()
    rows = []
    session = None
    for query, scenario, speaker in [
        ('Explain why the Moon has phases in simple terms.', 'classroom', 'theo'),
        ('Why do people need sleep to learn effectively?', 'classroom', 'theo'),
        ('How did Roman aqueducts carry water without electric pumps?', 'ancient_rome', 'aurelius'),
        ('What small step could help me feel less nervous meeting new classmates?', 'cafe', 'maya'),
        ('What is my name and favourite topic?', 'general_assistant', 'kai'),
    ]:
        started = time.perf_counter()
        first = sentence = None
        final = None
        with requests.post(args.url+'/chat/stream', json={
            'query': query, 'scenario': scenario, 'speaker_id': speaker, 'session_id': session,
            'world_context': 'Explicit shared preferences: visitor name=Robin; favourite topic=astronomy. Actual world='+scenario+'; no action has been completed.'
        }, stream=True, timeout=60) as response:
            response.raise_for_status()
            for line in response.iter_lines(chunk_size=1):
                if not line: continue
                packet = json.loads(line)
                if packet['event'] == 'error': raise RuntimeError(packet['text'])
                if packet['event'] == 'delta' and first is None: first = time.perf_counter()-started
                if packet['event'] == 'sentence' and sentence is None: sentence = time.perf_counter()-started
                if packet['event'] == 'done': final = packet['result']
        assert final and final['response_source'] in {'ollama', 'local-memory'}, final
        session = final['session_id']
        row = dict(query=query, first_token_s=first, first_sentence_s=sentence, total_s=time.perf_counter()-started,
                   answer=final['response'], source=final['response_source'], model=final.get('model','state lookup'))
        rows.append(row)
        print(json.dumps(row), flush=True)
    Path(args.output).write_text(json.dumps(rows, indent=2), encoding='utf-8')

if __name__ == '__main__': main()

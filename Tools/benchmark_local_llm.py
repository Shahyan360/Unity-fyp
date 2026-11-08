"""Reproducible real Ollama latency/answer samples; never calls a cloud model."""
import argparse
import json
import statistics
import time
from pathlib import Path
import requests

QUESTIONS = [
    "Explain why the Moon has phases in two short sentences.",
    "I feel nervous introducing myself to my classmates. What can I say?",
    "How did Roman aqueducts carry water without electric pumps?",
    "My name is Robin and I enjoy astronomy. Suggest a museum question for me.",
]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--models", nargs="+", default=["qwen3.5:2b-q4_K_M", "qwen3:0.6b", "qwen2.5:1.5b"])
    parser.add_argument("--output", required=True)
    parser.add_argument("--gpu-layers", type=int, default=-1)
    parser.add_argument("--url", default="http://127.0.0.1:11434")
    args = parser.parse_args()
    rows = []
    for model in args.models:
        # Unload previously benchmarked models so the comparison fits 4 GB GPUs.
        for item in requests.get(args.url+"/api/ps", timeout=5).json().get("models", []):
            requests.post(args.url+"/api/generate", json={"model": item["name"], "keep_alive": 0}, timeout=30).raise_for_status()
        for index, question in enumerate([QUESTIONS[0]] + QUESTIONS):
            started = time.perf_counter()
            first = None
            text = ""
            final = {}
            with requests.post(args.url+"/api/chat", json={
                "model": model, "stream": True, "think": False, "keep_alive": "30m",
                "options": {"num_ctx": 2048, "num_predict": 96, "temperature": .4, "num_gpu": args.gpu_layers},
                "messages": [{"role": "system", "content": "You are Kai, a friendly virtual learning guide. Answer accurately in at most two short sentences and 45 words. Do not use introductions or stage directions."},
                             {"role": "user", "content": question}]}, timeout=(5, 90), stream=True) as response:
                response.raise_for_status()
                for line in response.iter_lines(chunk_size=1):
                    if not line: continue
                    packet = json.loads(line)
                    if packet.get("error"): raise RuntimeError(packet["error"])
                    delta = packet.get("message", {}).get("content", "")
                    if delta and first is None: first = time.perf_counter() - started
                    text += delta
                    if packet.get("done"): final = packet
            row = dict(model=model, cold=index == 0, question=question, first_token_s=first,
                       total_s=time.perf_counter()-started, answer=text,
                       tokens_per_second=final.get("eval_count", 0)/max(final.get("eval_duration", 1)/1e9, .001))
            rows.append(row)
            print(json.dumps(row), flush=True)
        warm = [r for r in rows if r["model"] == model and not r["cold"]]
        print(model, "median TTFT", statistics.median(r["first_token_s"] for r in warm), flush=True)
        Path(args.output).parent.mkdir(parents=True, exist_ok=True)
        Path(args.output).write_text(json.dumps(rows, indent=2), encoding="utf-8")

if __name__ == "__main__": main()

"""Fit sparse question vectors for Lumen's local reference retrieval."""
import json
import math
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "embodied-ai-backend/data/knowledge"
STOP = set("a an the of in is did do does what how why was were for to and on it my can could you me".split())


def terms(text):
    return {word for word in re.findall(r"[a-z]+", text.lower()) if word not in STOP}


def vector(text, weights):
    features = {word: weights[word] for word in terms(text) if word in weights}
    norm = math.sqrt(sum(value * value for value in features.values())) or 1
    return {word: value / norm for word, value in features.items()}


def train(topics):
    samples = [question for topic in topics.values() for question in topic["train"]]
    counts = Counter(word for question in samples for word in terms(question))
    weights = {word: 1 + math.log((len(samples) + 1) / (count + 1)) for word, count in sorted(counts.items())}
    centroids = {}
    for key, topic in topics.items():
        center = Counter()
        for question in topic["train"]:
            center.update(vector(question, weights))
        norm = math.sqrt(sum(value * value for value in center.values()))
        centroids[key] = {
            "vector": {word: round(value / norm, 6) for word, value in sorted(center.items())},
            "scenarios": topic["scenarios"],
            "reference": topic["reference"],
        }
    return {"weights": weights, "topics": centroids}


if __name__ == "__main__":
    dataset = json.loads((ROOT / "Tools/guide_retriever_examples.json").read_text(encoding="utf-8"))
    model = train(dataset)
    out = DATA / "guide_retriever.json"
    out.write_text(json.dumps(model, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    import sys
    sys.path.insert(0, str(ROOT / "embodied-ai-backend"))
    from app import rank_guide_reference
    passed = total = 0
    for key, topic in dataset.items():
        for question in topic["held_out"]:
            actual, _ = rank_guide_reference(question, topic["scenarios"][0])
            passed += actual == key
            total += 1
            print(f"{'PASS' if actual == key else 'FAIL'} {key}: {question} -> {actual or 'abstained'}")
    for question in ("Tell me a joke", "Can you walk with me?", "How are you today?", "I forgot my wallet"):
        actual, _ = rank_guide_reference(question, "general_assistant")
        passed += not actual
        total += 1
        print(f"{'PASS' if not actual else 'FAIL'} unrelated: {question} -> {actual or 'abstained'}")
    print(f"Held-out and unrelated retrieval: {passed}/{total}")

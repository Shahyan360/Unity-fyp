"""Summarize raw release telemetry without treating missing counters as zero."""
import argparse
import csv
import json
import math
from collections import defaultdict
from pathlib import Path


def summary(values):
    data = sorted(float(x) for x in values if math.isfinite(float(x)) and float(x) >= 0)
    if not data:
        return {'samples': 0, 'status': 'unavailable'}
    return {'samples': len(data), 'p95': data[max(0, math.ceil(.95 * len(data)) - 1)],
            'p99': data[max(0, math.ceil(.99 * len(data)) - 1)], 'max': data[-1],
            'over100ms': sum(x > 100 for x in data)}


def metrics(directory):
    worlds = defaultdict(list)
    with (directory / 'frames.csv').open(newline='') as stream:
        for row in csv.DictReader(stream):
            if row['settling'] == '0':
                worlds[row['world']].append(row)
    result = {'definitions': {'wall_frame_ms': 'Update callback gap measured by Stopwatch; separate from engine timing',
                              'unity_frame_ms': 'Unity unscaled frame interval',
                              'cpu_frame_ms': 'FrameTimingManager CPU frame time',
                              'gpu_frame_ms': 'FrameTimingManager GPU frame time'},
              'percentiles': 'Nearest rank; negative and non-finite samples excluded',
              'gateStatus': 'No transferred performance protocol/targets found; measurements are not a pass certification',
              'machine': json.loads((directory / 'machine.json').read_text()), 'worlds': {}}
    for world, rows in worlds.items():
        result['worlds'][world] = {
            'allSteadyFrames': {k: summary(r[k] for r in rows) for k in result['definitions']},
            'focusedSteadyFrames': {k: summary(r[k] for r in rows if r['focused'] == '1') for k in result['definitions']},
            'speakingFrames': sum(r['speaking'] == '1' for r in rows),
            'gcGen0Collections': sum(int(r['gc_gen0_collection_delta']) for r in rows),
            'stalls': [{k: r[k] for k in ('elapsed_s', 'wall_frame_ms', 'unity_frame_ms', 'cpu_frame_ms',
                                         'gpu_frame_ms', 'focused', 'speaking', 'gc_gen0_collection_delta')}
                       for r in rows if float(r['wall_frame_ms']) > 100]}
    counters = directory / 'render-counters.csv'
    if counters.exists():
        with counters.open(newline='') as stream:
            rows = list(csv.DictReader(stream))
        result['renderCounters'] = {}
        for name in list(rows[0] if rows else {})[6:]:
            data = [float(r[name]) for r in rows if float(r[name]) >= 0]
            result['renderCounters'][name] = {'samples': len(data), 'min': min(data) if data else None,
                                               'max': max(data) if data else None,
                                               'varies': len(set(data)) > 1}
    return result


def soak(path):
    raw = json.loads(path.read_text())
    rows = raw.get('inventories', [])
    result = {'build': raw.get('build'), 'status': raw.get('status'), 'completedCycles': raw.get('completedCycles'),
              'definition': raw.get('cycleDefinition'), 'errors': raw.get('errors'), 'trends': {}}
    # Compare post-warmup Home samples with the final Home samples; never force GC.
    early, late = rows[7:12], rows[-5:]
    for key in ('allocatedBytes', 'monoBytes', 'graphicsDriverBytes', 'workingSetBytes',
                'materials', 'meshes', 'textures', 'audioClips', 'persistentObjects', 'audioSources', 'liveControllers'):
        if not early or not late or key not in early[0]:
            continue
        start = sum(r[key] for r in early) / len(early)
        end = sum(r[key] for r in late) / len(late)
        result['trends'][key] = {'cycles8to12Mean': start, 'last5Mean': end, 'delta': end - start}
    result['interpretation'] = 'Trend data requires review; completion alone does not prove absence of leaks.'
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--metrics', type=Path)
    parser.add_argument('--soak', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error('Preserve earlier summaries; choose a new output file.')
    report = {}
    if args.metrics:
        report['performance'] = metrics(args.metrics)
    if args.soak:
        report['soak'] = soak(args.soak)
    args.output.write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()

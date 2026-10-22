# Measured performance — corrected Windows candidate

Build `ce3ac243a7174daab6c13cdaa44a4763`; player checkpoint `8e00c9f`, backend
checkpoint `9a7498f`. The real-provider walkthrough passed 14 questions over
616.58 seconds. It exercised all five experiences, local actions, voice,
cancellation, memory and a restart of only the owned backend.

Windows 11 Pro 10.0.26200; i5-10210U; 7824 MiB reported system memory; Intel UHD;
driver 31.0.101.2134; Direct3D 11. All steady samples were focused and used
**1600×900, Balanced**. The machine receipt's initial 1280×720 is a startup size;
the actual per-frame dimensions establish the measured workload. The existing
Editor and ordinary user applications remained open; this was not an isolated
laboratory machine.

## Frame intervals and tail events

Nearest-rank percentiles. The capture excludes its declared world/model settling
windows and inventory instrumentation windows. Raw CSVs and all retained outliers
remain available under `Builds/Finalization-20261022/live-walk-03/metrics`.

| Experience | Steady samples | Unity interval p95 ms | p99 ms | Maximum ms | Update gaps >100 ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Home | 3161 | 41.91 | 44.55 | 206.80 | 1 |
| Rome | 1826 | 72.30 | 76.78 | 103.77 | 1 |
| Classroom | 2121 | 58.82 | 61.50 | 271.52 | 1 |
| Museum | 1675 | 73.26 | 78.55 | 1206.50 | 3 |
| Café | 1823 | 64.77 | 68.13 | 78.90 | 0 |

| Experience | Update callback gap p95 ms | FrameTiming CPU p95 ms | FrameTiming GPU p95 ms |
| --- | ---: | ---: | ---: |
| Home | 41.82 | 41.89 | 40.06 |
| Rome | 72.15 | 72.22 | 70.00 |
| Classroom | 58.78 | 58.92 | 57.09 |
| Museum | 73.24 | 73.27 | 71.05 |
| Café | 65.00 | 64.74 | 62.72 |

Update gaps use Stopwatch time between Update callbacks. Unity intervals use
unscaledDeltaTime. CPU/GPU timings are the latest available FrameTimingManager
samples; they need not describe the same instant as a callback-gap outlier.
These overlapping measures must not be added together or relabeled as each other.

The largest retained Museum gap was 1206.39 ms at elapsed 393.1021 s, with a
1206.50 ms Unity interval. Latest CPU/GPU readings were 63.80/178.75 ms. There
was no Gen0 collection recorded at that sample. It preceded the next resource
inventory at 393.3376 s; the prior inventory was at 388.2989 s. It remains a
real observed tail event. The available counters do not establish its cause;
a detailed trace is still needed to distinguish audio/network scheduling,
driver work and other main-thread or host stalls. The Classroom maximum was
271.52 ms. All six >100 ms callback gaps had a zero Gen0-collection delta.

## Actual rendering counters

The old aggregate Batches/Draw Calls/GC-byte recorders were unavailable in this
release player. Alternate native render recorders returned positive data and
changed with the world. They are sampled every 30 frames; these are sampled
steady maxima, not guaranteed maxima of every rendered frame.

| Experience | SRP Batcher draw calls | Triangles Count | SetPass calls | Shadow Casters Count |
| --- | ---: | ---: | ---: | ---: |
| Home | 909 | 426609 | 89 | 523 |
| Rome | 5533 | 6357799 | 104 | 3119 |
| Classroom | 1478 | 356049 | 65 | 965 |
| Museum | 1204 | 4221169 | 134 | 669 |
| Café | 2687 | 643925 | 76 | 1284 |

SRP Batcher draw calls are the named recorder, not an invented total across
different counter categories. Startup zeros are preserved in raw data and
excluded from this steady-state table. Graphics-driver memory and the in-player
working-set counter returned zero and are unavailable; zero does not establish
zero graphics or resident memory use.

## Interpretation

This hardware has substantial rendering cost at the measured settings, and
steady frame intervals are well above a 60 Hz interval. The original numerical
performance targets and baseline protocol were not transferred. There is no
performance-budget pass or verified optimization claim. Low quality and a lower
resolution are available controls, but this run does not measure their benefit.
Rendering/shadow cost in Rome and Museum, followed by the 1.2-second Museum
tail event, are concrete next profiling targets. Raw data and definitions are
preserved in `performance-03.json`; the issue matrix retains the unresolved gate.


## Thirty-cycle resource and Windows memory review

The corrected player completed 30 offline destination/Home cycles without
forced collection or explicit asset unloading, with zero harness errors.
This exercises local world/content lifecycle; it is not a 30-cycle online
connection or microphone soak. The separate live walkthrough covers only its
own 14 questions, reconnections and backend restart.

Compare the means of Home inventories after cycles 8–12 and 26–30:

| Measure | Cycles 8–12 | Cycles 26–30 | Change |
| --- | ---: | ---: | ---: |
| Unity allocated memory | 408.54 MiB | 408.46 MiB | -0.08 MiB |
| Managed used memory | 11.69 MiB | 13.08 MiB | +1.39 MiB |
| Windows private bytes | 780.24 MiB | 781.28 MiB | +1.04 MiB |
| Windows resident working set | 372.39 MiB | 302.32 MiB | -70.07 MiB |
| Windows handles | 922.6 | 930.4 | +7.8 |
| Windows threads | 48.2 | 48.4 | +0.2 |

Windows values come from an independent 1 Hz Process sampler for the exact
player PID/start identity. Each inventory matches the preceding sample within
one second; all ten comparison samples are present. OS working-set trimming
can lower residency without releasing allocations; the decreasing resident
value does not establish an optimization. In-player working-set and graphics
memory readings are unavailable and were not used as zero-memory evidence.

Post-warmup inventories remain at 192 materials, 121 meshes, 121 textures,
one audio clip, five persistent objects, three audio sources, zero playing
sources and one live controller. No accumulating native asset/audio/controller
count was observed in this bounded run. Managed usage and handles increased
slightly; no numerical tolerance was supplied, and this does not certify that
all leaks are absent. Final-five allocated and private-memory means do not
show substantial aggregate growth. Raw inventories and OS samples remain in
`soak-03`; the reproducible joined analysis is `final-evidence-summary-03.json`.

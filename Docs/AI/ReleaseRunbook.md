# Building and validating Lumen on another Windows computer

Use Unity 6000.6.2f1 with Windows build support and the committed package lock.
Do not upgrade the Editor or render pipeline as a setup workaround. This machine
has Windows 11, an i5-10210U, about 8 GB RAM and Intel UHD graphics; this is a
test environment, not a verified minimum hardware specification.

## Source setup

Follow `embodied-ai-backend/README.md` for Python 3.12, the virtual environment
and private Windows `GEMINI_API_KEY` configuration. Gemini requires Internet
access. No API key belongs in Unity, committed source, reports or launch scripts.
The recorded SDK is `google-genai==2.32.0`.

Start the backend with `Start-Lumen-Backend.cmd`, then open HomeScene and press
Play with offline mode disabled. Start with typed input before a microphone test.
`/health` and `/live/status` check service/configuration state; neither proves a
successful conversation. The historical Ollama routes are independent of Gemini.

## Fresh Windows build

Close other instances of this project before a batch build. Never close an
unrelated Editor or discard unsaved scene work. From the repository root:

```powershell
unity build . --target StandaloneWindows64 --execute-method LumenReleaseBuild.Build --editor-version 6000.6.2f1 --allow-dirty-build --no-tail
```

Set `LUMEN_BUILD_OUTPUT` to a new directory to choose the destination. The default
is a timestamped folder under `Builds`. Inspect `build-result.txt`, build GUID,
`build-messages.txt` and `asset-build-residency.tsv`. A command timeout is not a
successful build; the final build receipt is authoritative.

## Run the exact player harnesses

From the repository root, using its backend virtual environment:

```powershell
& '.\embodied-ai-backend\.venv\Scripts\python.exe' Tools/run_acceptance.py --player 'Builds\YOUR-BUILD\Lumen.exe' --suite functional --output 'Builds\Evidence\Functional-01'
```

Repeat with `--suite characters`, `conversation`, `live-walk`, `images` and `soak`, each with a fresh output
directory. Default private backend port is 5078; choose another `--port` if busy.
The runner refuses an occupied port and never restarts an existing service.
Live acceptance uses the dedicated restart window and the existing 14-question
budget. Do not interact during scene transitions. Natural microphones remain off.
Run performance separately from builds, renders and other heavy tasks.

```powershell
& '.\embodied-ai-backend\.venv\Scripts\python.exe' Tools/analyze_release_evidence.py --metrics 'Builds\Evidence\Live-01\metrics' --soak 'Builds\Evidence\Soak-01\harness\soak.json' --output 'Builds\Evidence\summary-01.json'
```

Preserve raw failed runs. Read actual applied resolutions and nonblank captures.
Review the 30-cycle post-warmup resource trends; completion is not a leak pass.
Unavailable timing/counter values are `-1`, not zero. Do not combine callback
gaps, engine frame intervals and CPU/GPU timings into one metric.

On this Unity/Windows player, the in-player working-set and graphics-driver
memory APIs returned zero. Treat those as unavailable. After the soak creates
its report, run this observer in a second PowerShell terminal before cycle 8:

```powershell
.\Tools\Observe-LumenSoakMemory.ps1 -ReportPath 'Builds\Evidence\Soak-01\harness\soak.json' -OutputPath 'Builds\Evidence\Soak-01\os-process-memory.jsonl'
```

It identifies that specific soak player, samples Windows resident/private memory
and handle/thread counts at 1 Hz, and stops when that process exits. It does not
stop the game or any service. Add `--os-memory` with that JSONL path to the
analyzer. The analyzer uses the preceding sample within two seconds of each Home
inventory, avoiding a sample taken after the next destination starts loading.
The 30-cycle harness is explicitly offline; it does not certify a 30-cycle
provider/socket soak. The separate live journey exercises provider reconnection.

## Private portable package

Only package an actual fresh player build:

```powershell
& '.\embodied-ai-backend\.venv\Scripts\python.exe' Tools/package_release.py --build 'Builds\YOUR-BUILD' --output 'Builds\Lumen Package'
```

Optional already-installed offline dependencies use `--stt-model` and `--piper`.
Nothing is downloaded automatically. Supply their licenses/cards before including
them. The default package supports Gemini online; local speech models are absent.
Offline curated text, local controls, navigation and classroom exhibits do not
require Gemini. Offline microphone transcription requires the optional speech
model/runtime. Speech output can use installed Windows System.Speech voices as
a fallback when Piper is absent; test voice availability on the target machine.

Copy the entire package and launch `Start-Lumen.cmd`. Keep the launcher open;
closing the player shuts down only the backend started by that launcher. To use
another port, invoke `Tools/Start-Lumen.ps1 -Port 5079` from PowerShell. Launch
from outside the package and from a path containing spaces during acceptance.
`-CheckOnly` reports runtime/player/service discovery without starting them.

Launcher backend logs go to `%LOCALAPPDATA%\Lumen\Logs`. Explicit acceptance
logs go into its chosen output folder. Normal Unity player logs are normally
under LocalLow using the configured company/product; confirm the path on the
target machine rather than relying on the previous computer's username.

If startup fails: check port occupancy without stopping unrelated services,
the complete Runtime/Python folder, backend logs and `/live/status`. For provider
errors, check the key's model access, quota and Internet connection. Missing
credentials should produce an honest unavailable response, not a fabricated reply.

For the corrected private package, actual launcher acceptance receipts are in
`Builds/Finalization-20261022/Packaged-functional-03`,
`Packaged-conversation-03` and `Packaged-images-03`. Their `launcher-config.json`
records the exact argument array and external working directory. Reproduce with
a **new output directory**, copied isolated preferences and the corresponding
`-lumen-functional-dir`, `-lumen-conversation-dir` or `-lumen-images-dir` flag
passed through `Start-Lumen.ps1 -PlayerArguments`. Preserve the old evidence.
The offline functional run began before the image-only relevance change; online
and image runs use the updated module. Player/scene inputs stayed unchanged.
The launcher fixture test is separate from these actual game runs.

For an image relevance regression, enable images and ask “Show me the DNA model
and explain DNA simply.” Check the visible image and its credit, not only the
spoken answer. The earlier failure was a scientist portrait; the final lookup
normalizes presentation instructions and rejects titles without topic overlap.
For the unresolved performance tail, run the 14-question live walkthrough and
inspect raw Museum Update gaps/Unity intervals. Record the actual hardware,
viewport and quality. A detailed profiler trace is needed before assigning a
cause; a diagnostic development build must stay separate from release timings.

## Human audio checks still needed

With the final player, enable the microphone, ask a short question in a quiet
room, then interrupt actual speech. Confirm old audio stops and no late reply
appears. Repeat with ordinary background noise and speakers/headphones, switch
guide/world during a reply, disconnect/reconnect the microphone, and confirm
recovery. Record device names, errors, expected/actual behavior and build GUID.
Ask the listener to judge intelligibility and voice quality; synthetic tests
cannot certify those properties.

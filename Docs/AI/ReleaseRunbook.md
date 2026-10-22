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

Repeat with `--suite live-walk`, `images` and `soak`, each with a fresh output
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

## Human audio checks still needed

With the final player, enable the microphone, ask a short question in a quiet
room, then interrupt actual speech. Confirm old audio stops and no late reply
appears. Repeat with ordinary background noise and speakers/headphones, switch
guide/world during a reply, disconnect/reconnect the microphone, and confirm
recovery. Record device names, errors, expected/actual behavior and build GUID.
Ask the listener to judge intelligibility and voice quality; synthetic tests
cannot certify those properties.

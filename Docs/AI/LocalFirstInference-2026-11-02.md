# Local-first conversation implementation — 2 November 2026

The local LLM writes normal conversational and educational answers. Gemini Live
is no longer selected merely because an API key exists. World commands, scene
changes, exhibit selection, character identities and explicit preference recall
remain project-owned deterministic operations. Replies report `response_source`;
generated local replies also report the model and `cloud_answer=false`.

Selected default: **Qwen3.5 2B Q4_K_M**, Ollama tag `qwen3.5:2b-q4_K_M`.
This is the balance chosen from five locally tested candidates, not a claim
that it is universally the best model. The 0.6B candidate was faster but made
too many mistakes. The selected model still has small-model quality limits.

## Why this architecture

Calling Gemini and a local model in parallel does not make local inference faster.
Showing Gemini's answer first would make the cloud model the real answer engine.
The default therefore makes no Gemini answer requests. As updated on 5 November,
`LUMEN_CONVERSATION_PROVIDER=hybrid` uses local faster-whisper recognition and
Ollama answers for both microphone and typed input, then sends only answer text
to Gemini TTS for speech. Gemini Live is disabled in hybrid. The separate `gemini`
mode retains Live answer generation. Image retrieval remains a separate,
attributed network capability. Earlier Live voice measurements below describe
the historical implementation, not the current hybrid pipeline.

The project contributes character direction, bounded private history, explicit
visitor preferences, local knowledge retrieval, grounded world state, action
validation, navigation, cancellable streaming, speech integration, and provenance.
Using pretrained local weights is inference/integration, not training a new LLM.

## Hardware and runtime findings

Inspected: i7-1185G7 (four physical cores), 16 GB system RAM, T500 4 GB VRAM,
Ollama 0.40.13, Unity 6000.6.2f1. Ollama's default CUDA 13 discovery failed and
the old model produced no first token within a 90-second request. CPU inference
worked. An isolated server using `OLLAMA_LLM_LIBRARY=cuda_v12` recognized T500
and delivered working accelerated inference. The original Ollama service on
11434 was preserved; the project server uses 127.0.0.1:11435.

The backend launcher starts/reuses this server, loads the model before accepting
conversation, retains it for 30 minutes, and uses a 2048-token context with a
96-token answer budget and disabled thinking. Single-request scheduling reduces
VRAM contention. Direct `app.py` defaults to CPU on the normal Ollama port for
compatibility; use the launcher for the accelerated configuration.

## Measurements and reproducibility

Raw JSON/logs are in ignored `Builds/LocalLLM/`. No credentials are stored there.
`Tools/benchmark_local_llm.py` compares one unloaded-model request plus four warm
questions. `Tools/benchmark_conversation.py` exercises the real Flask stream,
sentence events, scenario personas and memory. These are small samples, not
population-level quality benchmarks or guaranteed performance under Unity load.

Initial measurements:

| Candidate | Warm median first token, direct model | Decode rate | Decision |
| --- | ---: | ---: | --- |
| Existing Llama 3.2 1B Q8, CPU | 0.713 s | approximately 10 tokens/s | Slow and factual mistakes |
| Qwen3 0.6B Q4, CUDA 12 | 0.106 s | approximately 106 tokens/s | Very fast, insufficient answer quality |
| Qwen2.5 1.5B Q4, CUDA 12 | 0.104 s | approximately 40 tokens/s | Fast; reference contradictions observed |
| Qwen3.5 0.8B Q8, CUDA 12 | approximately 0.168 s | approximately 36 tokens/s | Persona and factual mistakes observed |
| Qwen3.5 2B Q4, CUDA 12 | 0.309 s | approximately 28 tokens/s | Selected; better grounded samples, 1.60 GB GPU allocation |

Final real backend run: `Builds/LocalLLM/conversation-accepted.json`. Four generated
replies began in **1.13–1.52 s**, with first sentences at **1.76–2.24 s** and
complete replies in **2.07–2.88 s**. Saved-preference recall took **0.009 s**.
These samples include real prompt construction and HTTP streaming, not speech
recognition, synthesis, playback or a concurrently rendering Unity world.
The final sampling explicitly disables the model's presence penalty, which had
encouraged extraneous claims; temperature is 0.3. Local reference paragraphs
ground the sampled science/history answers. Full answer correctness remains
open-ended: earlier runs contain demonstrably incorrect statements and are
preserved. The final sampled answers are not proof of general accuracy.

Cold model startup took approximately 6–10 seconds; startup warmup keeps that
cost outside the usual visitor turn. The larger grounded prompts take longer
than the short direct-model benchmark. Preserve earlier flawed answers in the
JSON files; do not describe speed as proof of correctness.

Local speech checks: existing faster-whisper base.en model correctly recognized
a 1.34-second synthetic command as “Take me to the market.” The measured
recognition request took 2.63 seconds. Beam size is now one for lower latency.
Two subsequent warm recognition requests took 1.78 and 1.16 seconds and returned
the same correct transcript.
Windows local speech synthesized a sample sentence to WAV in 0.57 seconds;
optional Piper took 4.85 seconds in a separate cold process. These synthesis
measurements exclude playback and are not perceived microphone-to-voice latency.
Existing Windows speech remains the normal fallback. Human microphone/noise,
echo, device changes and perceived speech quality require a live acceptance pass.

## Start on this machine

Run `Tools/Start-LumenBackend.ps1` from PowerShell, then open the project in Unity
and press Play in HomeScene. `/conversation/config` must report `provider=local`.
`/health` shows the selected local model. `/voice/health` shows recognizer readiness.
M toggles listening; V provides push-to-talk. Typed chat also uses local streaming.

The launcher reads the saved Windows user settings `LUMEN_PYTHON` and
`LUMEN_STT_MODEL`. This machine uses the already-installed Python voice runtime
and base.en model on D:. Nothing requires a Gemini key in local mode. A custom
`OLLAMA_BASE_URL`, `OLLAMA_MODEL`, or `LUMEN_NUM_GPU` can override the defaults.
For another computer install the declared dependencies and local model; do not
copy these machine-specific settings blindly.

## Validation and remaining coverage

- Unity batch compilation passed twice; the final log ended with exit code 0
  (`Builds/LocalLLM/unity-final-compile.log`). A pre-existing Editor analyzer
  warning in LumenAudioAuthoring remains. No new compile errors were reported.
- Final backend suite: **93 passed**. Missing-player packaging guard also passed.
  The expensive relocated-runtime packaging test was excluded in the combined
  94-pass run; a new distributable was not built.
- Existing D: Python voice runtime imports and `pip check` passed. The C: virtual
  environment lacks the complete voice stack; the launcher selects the saved
  working D: runtime. An attempted C: full dependency download failed on a network
  reset; Pillow/flask-sock subsequently installed successfully there.
- Local speech recognition and both synthesis engines were exercised with files.
  Natural microphone recognition, speaker echo/barge-in and actual Unity player
  microphone-to-voice timing remain unverified. No fresh standalone player build
  or rendered-world performance/VRAM contention test was performed.
- Local backend on 5000 and project Ollama on 11435 are left running for testing.
  Temporary comparison backend on 5001 was stopped. Original Ollama was preserved.
- The pre-existing local TMP font edit was restored exactly after Unity imports;
  its stash backup remains. No scene or package changes were intended.

## Research sources

- [Ollama FAQ: context, memory, preloading, GPU and keep-alive](https://docs.ollama.com/faq)
- [Ollama thinking controls](https://docs.ollama.com/capabilities/thinking)
- [Ollama GPU support, including T500](https://github.com/ollama/ollama/blob/main/docs/gpu.mdx)
- [Qwen3 0.6B quantized model](https://ollama.com/library/qwen3:0.6b)
- [Qwen2.5 1.5B quantized model](https://ollama.com/library/qwen2.5:1.5b)
- [Qwen3.5 0.8B model card](https://huggingface.co/Qwen/Qwen3.5-0.8B)
- [Qwen3.5 2B Q4 model](https://ollama.com/library/qwen3.5:2b-q4_K_M)

Model size/quantization and supported controls come from the primary sources;
the machine-specific speed and backend selection decisions come from local tests.

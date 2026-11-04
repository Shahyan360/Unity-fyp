param([int]$Port = 11435)
$ErrorActionPreference = 'Stop'
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Invalid Ollama port.' }
$address = "http://127.0.0.1:$Port"
if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) {
    Invoke-RestMethod "$address/api/version" -TimeoutSec 3 | Out-Null
    return
}
$ollama = (Get-Command ollama -ErrorAction Stop).Source
$logDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) 'Builds\LocalLLM'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$previousHost = $env:OLLAMA_HOST
$previousLibrary = $env:OLLAMA_LLM_LIBRARY
$previousParallel = $env:OLLAMA_NUM_PARALLEL
try {
    $env:OLLAMA_HOST = "127.0.0.1:$Port"
    # CUDA 13 discovery hangs on this machine; CUDA 12 was benchmarked on T500.
    $env:OLLAMA_LLM_LIBRARY = 'cuda_v12'
    $env:OLLAMA_NUM_PARALLEL = '1'
    Start-Process -FilePath $ollama -ArgumentList 'serve' -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDirectory 'ollama.stdout.log') -RedirectStandardError (Join-Path $logDirectory 'ollama.stderr.log') | Out-Null
} finally {
    $env:OLLAMA_HOST = $previousHost
    $env:OLLAMA_LLM_LIBRARY = $previousLibrary
    $env:OLLAMA_NUM_PARALLEL = $previousParallel
}
$deadline = [DateTime]::UtcNow.AddSeconds(30)
do {
    try { Invoke-RestMethod "$address/api/version" -TimeoutSec 2 | Out-Null; return } catch { Start-Sleep -Milliseconds 250 }
} while ([DateTime]::UtcNow -lt $deadline)
throw "Local Ollama did not start. See $logDirectory."

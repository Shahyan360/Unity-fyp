param([switch]$CheckOnly, [int]$Port = 5000)
$ErrorActionPreference = 'Stop'
$backend = Join-Path (Split-Path -Parent $PSScriptRoot) 'embodied-ai-backend'
foreach ($setting in @('LUMEN_PYTHON','LUMEN_STT_MODEL')) {
    if (-not [Environment]::GetEnvironmentVariable($setting,'Process')) {
        [Environment]::SetEnvironmentVariable($setting,[Environment]::GetEnvironmentVariable($setting,'User'),'Process')
    }
}
$python = if ($env:LUMEN_PYTHON) { $env:LUMEN_PYTHON } else { Join-Path $backend '.venv\Scripts\python.exe' }
if (-not (Test-Path -LiteralPath $python)) {
    throw 'Backend environment missing. Follow embodied-ai-backend/README.md to install requirements-voice.txt.'
}
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Choose a port from 1024 to 65535.' }
# Read the existing private Windows setting; never write the key to a file or output.
if (-not $env:GEMINI_API_KEY) {
    $env:GEMINI_API_KEY = [Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')
}
if (-not $env:GEMINI_LIVE_MODEL) {
    $env:GEMINI_LIVE_MODEL = [Environment]::GetEnvironmentVariable('GEMINI_LIVE_MODEL', 'User')
}
if ($CheckOnly) {
    [pscustomobject]@{
        PythonAvailable = $true
        GeminiKeySaved = [bool]($env:GEMINI_API_KEY -or $env:GOOGLE_API_KEY)
        ServiceAddress = "http://127.0.0.1:$Port"
    }
    return
}
if ($env:LUMEN_CONVERSATION_PROVIDER -in @('gemini','hybrid') -and -not ($env:GEMINI_API_KEY -or $env:GOOGLE_API_KEY)) {
    throw 'Save GEMINI_API_KEY in your Windows user environment before starting. Never put it in Unity or source files.'
}
if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) {
    throw "Port $Port is already occupied. Check http://127.0.0.1:$Port/live/status before starting another service."
}
$previousPort = $env:FLASK_PORT
try {
    $env:FLASK_PORT = [string]$Port
    if (-not $env:OLLAMA_MODEL) { $env:OLLAMA_MODEL = 'qwen3.5:2b-q4_K_M' }
    if ($env:LUMEN_CONVERSATION_PROVIDER -ne 'gemini') {
        if (-not $env:OLLAMA_BASE_URL) {
            & (Join-Path $PSScriptRoot 'Start-LumenOllama.ps1')
            $env:OLLAMA_BASE_URL = 'http://127.0.0.1:11435'
            if (-not $env:LUMEN_NUM_GPU) { $env:LUMEN_NUM_GPU = '-1' }
        }
        $address = if ($env:OLLAMA_BASE_URL) { $env:OLLAMA_BASE_URL } else { 'http://127.0.0.1:11434' }
        $warmup = @{model=$env:OLLAMA_MODEL; messages=@(); keep_alive='30m'; think=$false; options=@{num_ctx=2048;num_gpu=[int]$env:LUMEN_NUM_GPU;num_thread=4}} | ConvertTo-Json -Depth 5
        Write-Host "Warming local model $env:OLLAMA_MODEL ..."
        Invoke-RestMethod "$address/api/chat" -Method Post -ContentType 'application/json' -Body $warmup -TimeoutSec 90 | Out-Null
    }
    Write-Host "Starting Lumen backend at http://127.0.0.1:$Port. Keep this window open; Ctrl+C stops it."
    & $python -I (Join-Path $backend 'run_local.py')
    if ($LASTEXITCODE -ne 0) { throw "Backend stopped with exit code $LASTEXITCODE." }
} finally {
    $env:FLASK_PORT = $previousPort
}

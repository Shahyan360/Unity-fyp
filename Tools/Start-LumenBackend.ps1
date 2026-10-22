param([switch]$CheckOnly, [int]$Port = 5000)
$ErrorActionPreference = 'Stop'
$backend = Join-Path (Split-Path -Parent $PSScriptRoot) 'embodied-ai-backend'
$python = Join-Path $backend '.venv\Scripts\python.exe'
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
if (-not ($env:GEMINI_API_KEY -or $env:GOOGLE_API_KEY)) {
    throw 'Save GEMINI_API_KEY in your Windows user environment before starting. Never put it in Unity or source files.'
}
if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) {
    throw "Port $Port is already occupied. Check http://127.0.0.1:$Port/live/status before starting another service."
}
$previousPort = $env:FLASK_PORT
try {
    $env:FLASK_PORT = [string]$Port
    Write-Host "Starting Lumen backend at http://127.0.0.1:$Port. Keep this window open; Ctrl+C stops it."
    & $python -I (Join-Path $backend 'run_local.py')
    if ($LASTEXITCODE -ne 0) { throw "Backend stopped with exit code $LASTEXITCODE." }
} finally {
    $env:FLASK_PORT = $previousPort
}

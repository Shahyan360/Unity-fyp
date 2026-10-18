param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = 'D:\LumenArtifacts\Services'

function Read-BackendHealth {
    try { Invoke-RestMethod -Uri 'http://127.0.0.1:5000/health' -TimeoutSec 3 } catch { $null }
}

# Use the project's existing app.py and existing service configuration.
# This launcher never installs, starts, replaces, or reconfigures Ollama.
$backend = Read-BackendHealth
if (-not $backend -and -not $CheckOnly) {
    $python = Join-Path $projectRoot 'embodied-ai-backend\.venv\Scripts\python.exe'
    $app = Join-Path $projectRoot 'embodied-ai-backend\app.py'
    if (-not (Test-Path -LiteralPath $python)) { throw 'The backend virtual environment is missing.' }
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Start-Process -FilePath $python -ArgumentList ('"' + $app + '"') -WorkingDirectory (Split-Path -Parent $app) `
        -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDirectory "backend-$stamp.log") `
        -RedirectStandardError (Join-Path $logDirectory "backend-$stamp-error.log") | Out-Null
    for ($attempt = 0; $attempt -lt 30 -and -not $backend; $attempt++) {
        Start-Sleep -Milliseconds 500
        $backend = Read-BackendHealth
    }
    if (-not $backend) { throw 'Backend did not start. Inspect the service logs on D.' }
}
[pscustomobject]@{ Backend = if ($backend) { $backend.status } else { 'Unavailable' } }

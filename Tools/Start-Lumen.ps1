param([switch]$CheckOnly,[string]$PlayerPath,[int]$Port=5000,[string[]]$PlayerArguments=@())
$ErrorActionPreference='Stop'
$releaseRoot=Split-Path -Parent $PSScriptRoot
$backendRoot=Join-Path $releaseRoot 'embodied-ai-backend'
$logRoot=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Lumen\Logs'
$python=$env:LUMEN_PYTHON
if(-not $python){$python=Join-Path $releaseRoot 'Runtime\Python\python.exe'}
if(-not (Test-Path -LiteralPath $python)){$python=Join-Path $backendRoot '.venv\Scripts\python.exe'}
if(-not $PlayerPath){$PlayerPath=Join-Path $releaseRoot 'Lumen.exe'}
if($Port -lt 1024 -or $Port -gt 65535){throw 'Choose a local service port from 1024 to 65535.'}
$address="http://127.0.0.1:$Port"
function Read-Health {try{Invoke-RestMethod -Uri "$address/health" -TimeoutSec 4}catch{$null}}
function Is-Lumen($health){$health -and $health.status -eq 'ok' -and $health.service -eq 'embodied-ai-local' -and $health.contract_version -ge 2}
function ConvertTo-LumenProcessArgument([string]$Value) {
    $escaped=[regex]::Replace($Value,'(\\*)"','$1$1\"')
    $escaped=[regex]::Replace($escaped,'(\\+)$','$1$1')
    return '"'+$escaped+'"'
}
function Stop-LumenOwnedService($rootProcess) {
    if(-not $rootProcess -or $rootProcess.HasExited){return}
    # A Windows venv interpreter can be a redirector with an actual Python child.
    # Establish descendants and creation identities, then stop only that owned tree.
    $all=@(Get-CimInstance Win32_Process -Property ProcessId,ParentProcessId,CreationDate)
    $rootRecord=$all|Where-Object {$_.ProcessId -eq $rootProcess.Id -and [Math]::Abs(($_.CreationDate-$rootProcess.StartTime).TotalSeconds) -lt 1}
    if(-not $rootRecord){Write-Warning 'Service identity changed; preserved the process.';return}
    $nodes=[System.Collections.Generic.List[object]]::new();$nodes.Add($rootRecord)
    for($index=0;$index -lt $nodes.Count;$index++) {
        foreach($child in $all|Where-Object {$_.ParentProcessId -eq $nodes[$index].ProcessId -and $_.CreationDate -ge $rootRecord.CreationDate}){$nodes.Add($child)}
    }
    for($index=$nodes.Count-1;$index -ge 0;$index--) {
        $record=$nodes[$index];$current=Get-CimInstance Win32_Process -Filter ('ProcessId='+$record.ProcessId) -Property ProcessId,CreationDate
        if($current -and $current.CreationDate -eq $record.CreationDate){Stop-Process -Id $record.ProcessId -ErrorAction SilentlyContinue}
    }
}
$health=Read-Health
if($CheckOnly){[pscustomobject]@{ServiceReady=[bool](Is-Lumen $health);PythonAvailable=(Test-Path -LiteralPath $python);PlayerAvailable=(Test-Path -LiteralPath $PlayerPath);ServiceAddress=$address};return}
if(-not(Test-Path -LiteralPath $PlayerPath)){throw 'Lumen.exe is missing. Run this launcher from the complete release folder.'}
$ownedService=$null;$playerExit=$null;$oldBackend=$env:LUMEN_BACKEND_URL;$oldPiper=$env:LUMEN_PIPER_HOME
try {
    if(-not(Is-Lumen $health)) {
        if($health -or (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue)){throw "Another service uses port $Port. Preserve it and choose a different -Port."}
        if(-not(Test-Path -LiteralPath $python)){throw 'The local Python runtime is missing. Set LUMEN_PYTHON to a supported installed interpreter or restore Runtime/Python.'}
        New-Item -ItemType Directory -Path $logRoot -Force|Out-Null
        $stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$priorPort=$env:FLASK_PORT;$priorModel=$env:LUMEN_STT_MODEL
        try {
            $env:FLASK_PORT=[string]$Port
            if(-not $env:LUMEN_STT_MODEL){$env:LUMEN_STT_MODEL=Join-Path $backendRoot 'runtime\models\base.en'}
            $ownedService=Start-Process -FilePath $python -ArgumentList @('-I',('"'+(Join-Path $backendRoot 'run_local.py')+'"')) -WorkingDirectory $backendRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logRoot "$stamp-service.log") -RedirectStandardError (Join-Path $logRoot "$stamp-service-error.log")
        } finally {$env:FLASK_PORT=$priorPort;$env:LUMEN_STT_MODEL=$priorModel}
        for($attempt=0;$attempt -lt 30 -and -not(Is-Lumen $health);$attempt++) {
            if($ownedService.HasExited){throw 'The local service stopped during startup. Inspect the latest Lumen service log in local application data.'}
            Start-Sleep -Milliseconds 500;$health=Read-Health
        }
        if(-not(Is-Lumen $health)){throw 'The local service did not become ready within its startup limit.'}
    }
    $env:LUMEN_BACKEND_URL=$address
    if(-not $env:LUMEN_PIPER_HOME){$env:LUMEN_PIPER_HOME=Join-Path $releaseRoot 'Runtime\Piper'}
    $launch=@{FilePath=$PlayerPath;WorkingDirectory=$releaseRoot;PassThru=$true}
    if($PlayerArguments.Count -gt 0){$launch.ArgumentList=($PlayerArguments|ForEach-Object {ConvertTo-LumenProcessArgument $_}) -join ' '}
    $player=Start-Process @launch
    $player.WaitForExit()
    $playerExit=$player.ExitCode
} finally {
    $env:LUMEN_BACKEND_URL=$oldBackend;$env:LUMEN_PIPER_HOME=$oldPiper
    # Stop only the child service created here; preserve any existing service.
    Stop-LumenOwnedService $ownedService
}
if($null -ne $playerExit -and $playerExit -ne 0){throw "Lumen stopped with exit code $playerExit. Inspect the player log."}

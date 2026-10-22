param([Parameter(Mandatory=$true)][string]$ReportPath,[Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
$reportFile=(Resolve-Path -LiteralPath $ReportPath).Path
$outputFile=[IO.Path]::GetFullPath($OutputPath)
if(Test-Path -LiteralPath $outputFile){throw 'Preserve existing process-memory evidence.'}
if(-not(Test-Path -LiteralPath (Split-Path -Parent $outputFile))){throw 'Create the output directory first.'}
$raw=Get-Content -LiteralPath $reportFile -Raw|ConvertFrom-Json
$profileDirectory=Split-Path -Parent $reportFile
$ownedCandidates=@(Get-CimInstance Win32_Process -Filter "Name='Lumen.exe'"|Where-Object {
    $_.CommandLine -match 'lumen-soak-dir' -and $_.CommandLine.IndexOf($profileDirectory,[StringComparison]::OrdinalIgnoreCase) -ge 0
})
if($ownedCandidates.Count -ne 1){throw 'Require exactly one player running this soak profile.'}
$process=Get-Process -Id $ownedCandidates[0].ProcessId
if([Math]::Abs(($process.StartTime-$ownedCandidates[0].CreationDate).TotalSeconds) -ge 1){throw 'Process identity changed.'}
$deadline=(Get-Date).AddMinutes(48)
try {
    while(-not $process.HasExited -and (Get-Date) -lt $deadline) {
        $process.Refresh()
        $now=[DateTimeOffset]::UtcNow
        [pscustomobject]@{
            utcUnixSeconds=$now.ToUnixTimeMilliseconds()/1000.0
            utc=$now.ToString('O');build=$raw.build;playerPid=$process.Id
            playerStartUtc=$process.StartTime.ToUniversalTime().ToString('O')
            workingSetBytes=$process.WorkingSet64;privateBytes=$process.PrivateMemorySize64
            handles=$process.HandleCount;threads=$process.Threads.Count
        }|ConvertTo-Json -Compress|Add-Content -LiteralPath $outputFile -Encoding utf8
        Start-Sleep -Milliseconds 1000
    }
} finally {$process.Dispose()}
Write-Output 'Observed player exited or observation deadline reached; sampling finished.'

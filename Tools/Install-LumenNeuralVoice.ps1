param([switch]$IncludeRecognition)
$ErrorActionPreference = 'Stop'
$root = Join-Path (Split-Path -Parent $PSScriptRoot) 'Runtime/Piper'
New-Item -ItemType Directory -Force "$root/voices" | Out-Null
if (-not (Test-Path "$root/piper/piper.exe")) {
    $zip = Join-Path $root 'piper.zip'
    Invoke-WebRequest 'https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip' -OutFile $zip
    Expand-Archive -LiteralPath $zip -DestinationPath $root -Force
}
foreach ($file in @('en_GB-vctk-medium.onnx','en_GB-vctk-medium.onnx.json','MODEL_CARD')) {
    $destination = Join-Path "$root/voices" $file
    if (-not (Test-Path -LiteralPath $destination)) {
        $partial = $destination + '.partial'
        Invoke-WebRequest "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/vctk/medium/$file" -OutFile $partial
        Move-Item -LiteralPath $partial -Destination $destination -Force
    }
}
Write-Host 'Local neural voice installed. Restart the Lumen backend to warm the voice.'

if ($IncludeRecognition) {
    $recognition = Join-Path (Split-Path -Parent $PSScriptRoot) 'embodied-ai-backend/runtime/models/base.en'
    New-Item -ItemType Directory -Force $recognition | Out-Null
    foreach ($file in @('config.json','tokenizer.json','vocabulary.txt','model.bin')) {
        $destination = Join-Path $recognition $file
        if (-not (Test-Path -LiteralPath $destination)) {
            $partial = $destination + '.partial'
            Invoke-WebRequest "https://huggingface.co/Systran/faster-whisper-base.en/resolve/main/$file" -OutFile $partial
            Move-Item -LiteralPath $partial -Destination $destination -Force
        }
    }
}

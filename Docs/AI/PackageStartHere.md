# Start Lumen

This package is for private acceptance. Public redistribution rights for some
legacy assets and fonts remain unresolved; see DISTRIBUTION_STATUS.md.

1. Extract/copy the **entire folder** to a writable local directory. Keep
   Lumen.exe, Lumen_Data, Runtime and the launcher together.
2. For online conversation, create/use your own Gemini API key and save it as
   the Windows **user environment variable** `GEMINI_API_KEY`. Do not put it in
   Unity, source files, chat or screenshots. Restart the launcher after changing
   configuration. The backend also accepts `GOOGLE_API_KEY`.
3. Double-click **Start-Lumen.cmd**. Keep its window open. The launcher starts
   the local service, opens Lumen, then stops only its own service when you exit.
4. Start with a typed question. Enable the microphone inside Lumen only when
   ready to test natural speech. Gemini conversation and live images need an
   Internet connection. The default live model is `gemini-3.8-live`.

Windows 11 x64 is the tested operating system. This computer has an i5-10210U,
about 8 GB RAM and Intel UHD graphics; a guaranteed minimum specification has
not been established. No separate Python install is needed when Runtime/Python
is included. Clean-user/device testing remains a separate acceptance check.

Offline mode retains local navigation, exhibits, local controls and curated text.
The default package does not contain Whisper speech models or Piper/eSpeak
voices. Offline microphone transcription needs its optional model/runtime.
Local speech output can fall back to installed Windows System.Speech voices;
availability and quality must be checked on the target computer. Optional Piper
components need their required notices. Read RUNTIME_INVENTORY.json for
the exact components in this package. Online Gemini audio does not require them.

If port 5000 is already occupied, leave the unrelated service running. From
PowerShell, run `Tools/Start-Lumen.ps1 -Port 5079`. For configuration checks, open
the local `/health` and `/live/status` URLs on the chosen port. A configured key
does not prove model access or available quota; an actual conversation does.

For launch errors, inspect `%LOCALAPPDATA%\Lumen\Logs`. Normal Unity player logs
are under LocalLow for the configured company/product. Acceptance runs use their
explicit output directories. A missing key, unavailable provider, or lost network
should show an unavailable state; retry after fixing configuration/connection.

Keep the build GUID, source identity and PACKAGE_SHA256.txt with any test report.
Do not interpret synthetic audio tests as acceptance of human microphones,
background noise, speaker echo or voice quality.

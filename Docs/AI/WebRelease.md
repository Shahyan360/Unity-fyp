# Lumen web release handoff

The browser target uses the four existing Unity scenes. Build with Unity 6000.6.2f1 and its **Web Build Support** module. The host must serve the Unity files and proxy `/api/` to the private Flask service on the same HTTPS origin. For example, a request to `https://lumen.example/api/chat/stream` forwards to the Flask `/chat/stream` route. The browser never connects directly to a visitor’s localhost or to Ollama.

## Build

Run the following from the project root after adding the module in Unity Hub. Choose an empty output directory. Unity writes the player and build reports there.

```powershell
$env:LUMEN_BUILD_OUTPUT = 'D:\Lumen-Web-Release'
& 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath $PWD.Path -buildTarget WebGL -executeMethod LumenReleaseBuild.BuildWeb -logFile 'D:\Lumen-Web-build.log'
```

## Host

- Serve the Unity player at the site’s root or a subdirectory. Keep its generated `Build` and `TemplateData` paths intact. Set the MIME type of WebAssembly to `application/wasm` and use the `Content-Encoding` corresponding to the Unity build’s compression format. [Unity server configuration](https://docs.unity3d.com/Manual/webgl-deploying.html)
- Set `LUMEN_WEB_ORIGIN` to the exact HTTPS origin. Keep Ollama and Flask behind the reverse proxy. Run Flask with a production WSGI server and disable proxy buffering on `/api/chat/stream`. Provide rate limiting and whichever access control applies to the intended audience before exposing model generation and transcription.
- For browser conversations, use `LUMEN_CONVERSATION_PROVIDER=local`. Browser voice uses the browser’s speech synthesis voices. If microphone permission is granted, the recording goes to the configured Lumen server for transcription. Text remains available if the visitor declines permission. The Windows launcher continues to support local and hybrid speech.
- Before sharing the URL, check the actual player in desktop Chrome or Edge and Firefox. Exercise startup, all destinations, typed conversation, microphone permission and denial, transcription, speech cancellation, pointer lock, and returning to the hub. Record startup transfer size and response latency through the deployed proxy.

## Release status

The machine’s Windows Unity installation initially had Windows build support only. The signed Web Build Support installer required administrator approval. Browser playback and deployment remain release gates until a Web build runs against the intended HTTPS host.

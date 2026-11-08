mergeInto(LibraryManager.library, {
  $LumenBrowserMic: {
    capture: null,
    send: function (capture, method, value) {
      if (LumenBrowserMic.capture === capture) SendMessage(capture.receiver, method, value);
    },
    stop: function (submit) {
      var capture = LumenBrowserMic.capture;
      if (!capture) return;
      if (!submit) LumenBrowserMic.capture = null;
      clearTimeout(capture.timer);
      if (capture.processor) capture.processor.disconnect();
      if (capture.source) capture.source.disconnect();
      if (capture.stream) capture.stream.getTracks().forEach(function (track) { track.stop(); });
      if (capture.audio && capture.audio.state !== 'closed') capture.audio.close().catch(function () {});
      if (!submit || capture.sending) return;
      capture.sending = true;
      if (!capture.count) {
        LumenBrowserMic.send(capture, 'BrowserTranscript', '');
        return;
      }
      LumenBrowserMic.send(capture, 'BrowserListening', 'Transcribing');
      var samples = new Float32Array(capture.count);
      var offset = 0;
      capture.parts.forEach(function (part) { samples.set(part, offset); offset += part.length; });
      var ratio = capture.rate / 16000;
      var length = Math.floor(samples.length / ratio);
      var wav = new DataView(new ArrayBuffer(44 + length * 2));
      var writeText = function (position, value) {
        for (var i = 0; i < value.length; i++) wav.setUint8(position + i, value.charCodeAt(i));
      };
      writeText(0, 'RIFF'); wav.setUint32(4, 36 + length * 2, true);
      writeText(8, 'WAVEfmt '); wav.setUint32(16, 16, true);
      wav.setUint16(20, 1, true); wav.setUint16(22, 1, true);
      wav.setUint32(24, 16000, true); wav.setUint32(28, 32000, true);
      wav.setUint16(32, 2, true); wav.setUint16(34, 16, true);
      writeText(36, 'data'); wav.setUint32(40, length * 2, true);
      for (var i = 0; i < length; i++) {
        var start = Math.floor(i * ratio);
        var end = Math.max(start + 1, Math.floor((i + 1) * ratio));
        var sum = 0;
        for (var j = start; j < end; j++) sum += samples[j];
        wav.setInt16(44 + i * 2, Math.max(-1, Math.min(1, sum / (end - start))) * 32767, true);
      }
      fetch(capture.endpoint, { method: 'POST', headers: { 'Content-Type': 'audio/wav' }, body: wav.buffer })
        .then(function (response) { if (!response.ok) throw new Error('Transcription request failed'); return response.json(); })
        .then(function (result) { LumenBrowserMic.send(capture, 'BrowserTranscript', result.text || ''); })
        .catch(function () { LumenBrowserMic.send(capture, 'BrowserMicrophoneError', 'Transcription is unavailable. You can type instead.'); });
    }
  },
  LumenRecordStart__deps: ['$LumenBrowserMic'],
  LumenRecordStart: function (receiverPtr, endpointPtr) {
    LumenBrowserMic.stop(false);
    var capture = { receiver: UTF8ToString(receiverPtr), endpoint: UTF8ToString(endpointPtr), parts: [], count: 0 };
    LumenBrowserMic.capture = capture;
    var Audio = window.AudioContext || window.webkitAudioContext;
    if (!Audio || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      LumenBrowserMic.send(capture, 'BrowserMicrophoneError', 'Microphone access requires HTTPS or localhost.');
      return;
    }
    capture.audio = new Audio();
    var started = capture.audio.resume();
    navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1, echoCancellation: true } }).then(function (stream) {
      if (LumenBrowserMic.capture !== capture || capture.sending) {
        stream.getTracks().forEach(function (track) { track.stop(); });
        return;
      }
      capture.stream = stream;
      return started.then(function () {
        if (LumenBrowserMic.capture !== capture || capture.sending) return;
        capture.rate = capture.audio.sampleRate;
        capture.source = capture.audio.createMediaStreamSource(stream);
        capture.processor = capture.audio.createScriptProcessor(2048, 1, 1);
        capture.processor.onaudioprocess = function (event) {
          if (capture.sending) return;
          var part = new Float32Array(event.inputBuffer.getChannelData(0));
          capture.parts.push(part);
          capture.count += part.length;
        };
        capture.source.connect(capture.processor);
        capture.processor.connect(capture.audio.destination);
        capture.timer = setTimeout(function () { LumenBrowserMic.stop(true); }, 12000);
        LumenBrowserMic.send(capture, 'BrowserListening', 'Listening. Click the microphone again to send.');
      });
    }).catch(function () {
      LumenBrowserMic.send(capture, 'BrowserMicrophoneError', 'Microphone access was unavailable. You can type instead.');
      if (LumenBrowserMic.capture === capture) LumenBrowserMic.stop(false);
    });
  },
  LumenRecordStop__deps: ['$LumenBrowserMic'],
  LumenRecordStop: function (submit) { LumenBrowserMic.stop(!!submit); },
  LumenSpeak: function (textPtr, volume, rate, pitch) {
    if (!window.speechSynthesis) return;
    var utterance = new SpeechSynthesisUtterance(UTF8ToString(textPtr));
    utterance.volume = volume;
    utterance.rate = rate;
    utterance.pitch = pitch;
    window.speechSynthesis.speak(utterance);
  },
  LumenCancelSpeech: function () { if (window.speechSynthesis) window.speechSynthesis.cancel(); },
  LumenIsSpeaking: function () { return window.speechSynthesis && window.speechSynthesis.speaking ? 1 : 0; }
});

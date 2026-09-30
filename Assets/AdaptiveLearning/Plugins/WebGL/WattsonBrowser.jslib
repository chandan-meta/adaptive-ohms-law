mergeInto(LibraryManager.library, {
  WattsonWebListen: function(target) {
    var name = UTF8ToString(target);
    var Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!Recognition) { SendMessage(name,'VoiceError','Speech recognition is unavailable in this browser. Try Chrome or Edge, or type your question.'); return; }
    if(window.wattsonRecognition) window.wattsonRecognition.abort();
    var recognition = new Recognition();
    window.wattsonRecognition = recognition;
    recognition.lang='en-US'; recognition.continuous=true; recognition.interimResults=false;
    var heard='';
    recognition.onstart=function(){SendMessage(name,'VoiceReady','');};
    recognition.onresult=function(event){
      for(var i=event.resultIndex;i<event.results.length;i++) if(event.results[i].isFinal) heard += event.results[i][0].transcript+' ';
      if(heard.trim()) SendMessage(name,'VoiceResult',heard.trim());
    };
    recognition.onerror=function(event){if(event.error!=='aborted') SendMessage(name,'VoiceError','Browser microphone: '+event.error+'. Check site microphone permission and your input device.');};
    recognition.onend=function(){if(window.wattsonRecognition===recognition){window.wattsonRecognition=null;if(!heard.trim())SendMessage(name,'VoiceError','No speech detected. Hold the microphone and speak, then release.');}};
    try { recognition.start(); } catch(error) { SendMessage(name,'VoiceError','Microphone could not start. Check browser microphone permission.'); }
  },
  WattsonWebRelease: function() { if(window.wattsonRecognition) window.wattsonRecognition.stop(); },
  WattsonWebCancel: function() { var recognition=window.wattsonRecognition;window.wattsonRecognition=null;if(recognition){recognition.onend=null;recognition.onerror=null;recognition.onresult=null;recognition.abort();} },
  WattsonWebSpeak: function(text) {
    if (!window.speechSynthesis) return;
    window.speechSynthesis.cancel();
    var utterance = new SpeechSynthesisUtterance(UTF8ToString(text));
    utterance.lang = 'en-US';
    utterance.rate = 0.95;
    window.speechSynthesis.speak(utterance);
  },
  WattsonWebStopSpeech: function() { if (window.speechSynthesis) window.speechSynthesis.cancel(); },
  WattsonWebSpeaking: function() { return window.speechSynthesis && window.speechSynthesis.speaking ? 1 : 0; }
});

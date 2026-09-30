package com.adaptiveohmslaw;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.speech.*;
import android.speech.tts.TextToSpeech;
import com.unity3d.player.UnityPlayer;
import java.util.ArrayList;
import java.util.Locale;

/** All platform speech APIs run on Android's UI thread. No credentials in the player. */
public final class WattsonVoice implements RecognitionListener {
    private final Activity activity;
    private final String receiver;
    private SpeechRecognizer recognizer;
    private TextToSpeech tts;
    private boolean ready, listening;
    private volatile boolean disposed;
    private String queued;
    public WattsonVoice(Activity activity,String receiver) {
        this.activity=activity;this.receiver=receiver;
        activity.runOnUiThread(()-> {
            if(disposed)return;
            tts=new TextToSpeech(activity,status->{
                if(disposed)return;
                ready=status==TextToSpeech.SUCCESS;
                if(ready){int language=tts.setLanguage(Locale.ENGLISH);ready=language>=0;}
                if(!ready){error("Install an English device voice to hear Wattson. Subtitles remain available.");queued=null;}
                else if(queued!=null){String text=queued;queued=null;speak(text);}
            });
        });
    }
    private void error(String text){if(!disposed)UnityPlayer.UnitySendMessage(receiver,"VoiceError",text);}
    public void speak(String text){activity.runOnUiThread(()->{if(disposed)return;if(!ready){queued=text;return;}tts.speak(text,TextToSpeech.QUEUE_FLUSH,null,"wattson");});}
    public void stopSpeech(){activity.runOnUiThread(()->{queued=null;if(tts!=null)tts.stop();});}
    public void listen(){activity.runOnUiThread(()->{
        if(disposed)return;
        if(!SpeechRecognizer.isRecognitionAvailable(activity)){error("No speech recognition service is installed. Type your question.");return;}
        try {
            if(tts!=null)tts.stop();
            if(recognizer==null){recognizer=SpeechRecognizer.createSpeechRecognizer(activity);recognizer.setRecognitionListener(this);}
            Intent intent=new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
            intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
            intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE,"en-US");intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS,1);
            listening=true;recognizer.startListening(intent);
        }catch(Exception e){error("Microphone could not start. Check permissions or type your question.");}
    });}
    public void finishListening(){activity.runOnUiThread(()->{if(recognizer!=null&&listening)recognizer.stopListening();});}
    public void stopListening(){activity.runOnUiThread(()->{listening=false;if(recognizer!=null)recognizer.cancel();});}
    public void dispose(){disposed=true;activity.runOnUiThread(()->{if(recognizer!=null){recognizer.destroy();recognizer=null;}if(tts!=null){tts.stop();tts.shutdown();tts=null;}});}
    public void onResults(Bundle results){ArrayList<String> words=results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);if(!disposed&&words!=null&&!words.isEmpty())UnityPlayer.UnitySendMessage(receiver,"VoiceResult",words.get(0));else error("No question heard. Hold the microphone to try again.");}
    public void onError(int code){if(listening){listening=false;String message;
        switch(code){
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS: message="Microphone permission is denied. In Android Settings > Apps > this app > Permissions, allow Microphone, then hold the microphone again.";break;
            case SpeechRecognizer.ERROR_NETWORK: case SpeechRecognizer.ERROR_NETWORK_TIMEOUT: message="The device speech service could not connect. Check your internet connection, then hold the microphone again.";break;
            case SpeechRecognizer.ERROR_AUDIO: message="The microphone could not record audio. Check the connected microphone and close other recording apps, then retry.";break;
            case SpeechRecognizer.ERROR_NO_MATCH: case SpeechRecognizer.ERROR_SPEECH_TIMEOUT: message="No clear question was heard. Hold the microphone, then speak near the microphone.";break;
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY: message="The device speech service is busy. Wait a moment, then hold the microphone again.";break;
            default: message="The device speech service stopped ("+code+"). Hold the microphone to retry, or type your question.";
        }error(message);}}
    public void onReadyForSpeech(Bundle b){UnityPlayer.UnitySendMessage(receiver,"VoiceReady","");} public void onBeginningOfSpeech(){} public void onRmsChanged(float r){if(listening)UnityPlayer.UnitySendMessage(receiver,"VoiceLevel",Float.toString(r));} public void onBufferReceived(byte[] b){} public void onEndOfSpeech(){} public void onPartialResults(Bundle b){} public void onEvent(int e,Bundle b){}
}


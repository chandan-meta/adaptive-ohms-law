param([Parameter(Mandatory=$true)][string]$ResultPath,[Parameter(Mandatory=$true)][string]$StatusPath,[string]$WavePath)
$ErrorActionPreference='Stop'
$taskEngine=$null
try {
    Add-Type -AssemblyName System.Speech
    $taskRecognizer=[System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | Where-Object {$_.Culture.Name -like 'en-*'} | Select-Object -First 1
    if($null -eq $taskRecognizer){throw 'NO_ENGLISH_RECOGNIZER'}
    $taskEngine=New-Object System.Speech.Recognition.SpeechRecognitionEngine($taskRecognizer)
    $taskGrammar=New-Object System.Speech.Recognition.DictationGrammar
    $taskEngine.LoadGrammar($taskGrammar)
    # Bias common lesson questions without disabling free-form dictation.
    $taskPhrases=New-Object System.Speech.Recognition.Choices
    $taskPhrases.Add([string[]]@('What is voltage','What is current','What is resistance','What is potential difference','What is Ohms law','What does an ammeter measure','What does a voltmeter measure','Why is the current zero','What should I do next','I do not understand','I dont understand','How do I plot a point','Which axes do I use','What does this pattern mean','Why does the current increase','What happens when voltage doubles','How do I turn the knob','How do I close the switch','Explain series connection','Explain parallel connection'))
    $taskBuilder=New-Object System.Speech.Recognition.GrammarBuilder
    $taskBuilder.Culture=$taskRecognizer.Culture
    $taskBuilder.Append($taskPhrases)
    $taskTopicGrammar=New-Object System.Speech.Recognition.Grammar($taskBuilder)
    $taskTopicGrammar.Name='Wattson lesson questions'
    $taskTopicGrammar.Priority=1
    $taskEngine.LoadGrammar($taskTopicGrammar)
    $taskEngine.InitialSilenceTimeout=[TimeSpan]::FromSeconds(12)
    $taskEngine.BabbleTimeout=[TimeSpan]::FromSeconds(12)
    $taskEngine.EndSilenceTimeout=[TimeSpan]::FromSeconds(1)
    if($WavePath){$taskEngine.SetInputToWaveFile($WavePath)}else{$taskEngine.SetInputToDefaultAudioDevice()}
    [IO.File]::WriteAllText($StatusPath,'Listening')
    $taskRecognition=$taskEngine.Recognize([TimeSpan]::FromSeconds(18))
    if($null -eq $taskRecognition){$taskResult=@{text='';error='NO_SPEECH';code=0}}else{$taskResult=@{text=$taskRecognition.Text;error='';code=0}}
} catch {
    $taskFailure=$_.Exception
    while($taskFailure.InnerException){$taskFailure=$taskFailure.InnerException}
    $taskResult=@{text='';error=$(if($taskFailure.Message -eq 'NO_ENGLISH_RECOGNIZER'){'NO_ENGLISH_RECOGNIZER'}else{'DEVICE_ERROR'});code=$taskFailure.HResult}
} finally {if($null -ne $taskEngine){$taskEngine.Dispose()}}
[IO.File]::WriteAllText($ResultPath,($taskResult | ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))

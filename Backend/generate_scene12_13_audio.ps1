Add-Type -AssemblyName System.Speech
$folder = "D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Audio\GeneratedVoice"
if (-not (Test-Path $folder)) { New-Item -ItemType Directory -Path $folder -Force }

$lines = @(
    @{ name='Scene12Dialogue_intro_voltage.wav'; text='We have seen what pushes charge.' },
    @{ name='Scene12Dialogue_intro_current.wav'; text='And we have measured how much charge flows.' },
    @{ name='Scene12Dialogue_intro_why.wav'; text='But why does not charge flow equally easily through every conductor?' },
    @{ name='Scene12Dialogue_inspect_prompt.wav'; text='Select wire XY to inspect its microscopic structure.' },
    @{ name='Scene12Dialogue_wire_opposes.wav'; text='The material opposes the flow of electric current.' },
    @{ name='Scene12Dialogue_wire_resistance_name.wav'; text='That opposition is called electrical resistance. Symbol R.' },
    @{ name='Scene12Dialogue_table_intro.wav'; text='And the best part? You have already collected the measurements we need.' },
    @{ name='Scene12Dialogue_table_constant.wav'; text='The value of V divided by I is the same for every reading. For this wire at constant temperature, that constant is its resistance.' },
    @{ name='Scene12Dialogue_graph_intro.wav'; text='There is another place we can find the same resistance.' },
    @{ name='Scene12Dialogue_graph_slope_explain.wav'; text='On a graph with voltage vertically and current horizontally, the slope gives us resistance. We found the same five ohms in the table and in the graph.' },
    @{ name='Scene12Dialogue_unit_reveal.wav'; text='A conductor has a resistance of one ohm when one volt across it produces a current of one ampere.' },
    @{ name='Scene12Dialogue_adaptive_question_prompt.wav'; text='Which calculation gives the resistance of our wire?' },
    @{ name='Scene12Dialogue_scene12_complete.wav'; text='Outstanding work! You have discovered electrical resistance and how to find it from measurements and graphs.' },
    @{ name='Scene12Dialogue_final_handoff.wav'; text='Now let us see how all three pieces come together to name the law.' },

    @{ name='Scene13Dialogue_intro_three_pieces.wav'; text='Now we know all three pieces.' },
    @{ name='Scene13Dialogue_piece_voltage.wav'; text='The electrical push. V, potential difference.' },
    @{ name='Scene13Dialogue_piece_current.wav'; text='The flow. I, current.' },
    @{ name='Scene13Dialogue_piece_resistance.wav'; text='The opposition. R, resistance.' },
    @{ name='Scene13Dialogue_assemble_prompt.wav'; text='Start with R equals V divided by I. Drag or tap the symbols into the slots to express voltage V.' },
    @{ name='Scene13Dialogue_law_naming.wav'; text='And the relationship connecting them is...' },
    @{ name='Scene13Dialogue_law_revealed.wav'; text='Ohms Law! V equals I times R.' },
    @{ name='Scene13Dialogue_challenge_prompt.wav'; text='Our wire has a resistance of five ohms. Set the voltage so the current becomes 0.60 amperes.' },
    @{ name='Scene13Dialogue_challenge_success.wav'; text='Spot on! At 3.0 volts across our five-ohm wire, the current is exactly 0.60 amperes.' },
    @{ name='Scene13Dialogue_condition_temp.wav'; text='For this conductor, while its temperature stays constant, voltage is directly proportional to current.' },
    @{ name='Scene13Dialogue_condition_constant.wav'; text='The constant connecting them is resistance.' },
    @{ name='Scene13Dialogue_finish_summary.wav'; text='We have got our law. Now let us return to the mystery that started all of this.' }
)

$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.Rate = 0
try {
    foreach ($item in $lines) {
        $outPath = Join-Path $folder $item.name
        $synth.SetOutputToWaveFile($outPath)
        $synth.Speak($item.text)
        Write-Host "Generated: $($item.name)"
    }
} finally {
    $synth.Dispose()
}
Write-Host "COMPLETE"

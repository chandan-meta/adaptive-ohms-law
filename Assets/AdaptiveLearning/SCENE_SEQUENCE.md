# Scene sequence aligned with the screenplay

AI video covers the historical narrative through Scene 7. The playable lab entry is now Scene_08_OurLabIntroduction.

- Scene_06_AdaptivePrediction is retained as an optional interactive prediction segment. Its transition now enters Scene 8; video integration is not provided here.
- Scene_08_OurLabIntroduction introduces the ammeter, voltmeter, nichrome wire XY, source and key. NEXT INSTRUMENT advances the voiced introduction.
- Scene_09_BuildTheCircuit uses two-terminal selection to assemble six leads. Incorrect topology is rejected with an explanation. Clues identify a remaining valid connection. The key cannot close prematurely. Correct completion displays 1.00 V and 0.20 A for the example 5-ohm conductor, records progress and enables the experiment transition.
- Previous Scene_07_OhmsExperiment is now Scene_10_BecomeTheExperimenter.
- Previous Scene_08_GraphEvidence is now Scene_11_TurnDataIntoGraph.

The renamed scenes retain their Unity asset GUIDs. Internal legacy class names, activity IDs and question IDs remain for saved-profile compatibility. Their existing experiment/graph mechanics are preserved; the later migration to five measurements at 5 ohms and voltage-on-vertical graph axes is still required. Do not consider these old activities fully screenplay-aligned merely because their scene numbers changed.

Scenes 8 and 9 now use saved, editable 3D scene objects with serialized LabSceneView references. LabLessonDefinition assets supply their lesson content. Wattson uses the existing prefab, voice synthesis, contextual chat and hold-to-talk system. No external art was added. Device models and bench remain placeholders. Camera is fixed and tablet controls share the same Unity UI event system as desktop.

Scene 9 records connection mistakes, hints, active response time and completion in circuitAssemblies. Help has no mastery penalty. Two mistakes trigger guided highlighting. No fabricated assessment score is awarded for merely viewing the introduction.

Open Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity and press Play. Build Settings begins with Scene 8, followed by 9, 10 and 11. Optional Scene 6 and existing SampleScene remain registered.

Validation on 29 September 2026: 14 Unity Play-mode checks passed in an isolated project copy, including introduction-to-assembly transition, invalid short rejection, six valid leads, switch closure, saved completion and mistakes, chat open/close, and shared hold-to-talk presence. Ten standalone topology checks passed. Rendered layouts inspected at 1920x1080, 1280x800 and 1440x1080. These checks do not substitute for a physical Android touch/voice test. Unity's isolated editor emitted a SearchDatabase startup exception; it did not prevent lesson checks completing. No lesson-script exception was found in the final run. Existing Scenes 10–11 were preserved rather than revalidated as screenplay-complete. No updated standalone player build was produced in this session.


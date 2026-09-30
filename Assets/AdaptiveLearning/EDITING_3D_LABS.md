# Editing the 3D labs

Scenes 8 and 9 now contain saved scene objects before Play. The equipment, camera and environment are not recreated when the lesson starts.

Open Scene_08_OurLabIntroduction or Scene_09_BuildTheCircuit. Exit Play and reopen the scene if Unity still shows the previous runtime-only hierarchy.

Hierarchy:
- Environment: workbench, legs, room, shelves and lighting.
- Equipment - edit models here: source, ammeter, nichrome wire, voltmeter and switch prefab instances. Expand each to edit meshes, materials, terminal controls and wire anchors.
- Characters: existing Wattson prefab.
- LearnerCamera: editable perspective framing.
- Instrument labels: editable world labels.
- Circuit leads - editable waypoints: six saved lead paths. Line renderers are disabled until the learner connects the corresponding terminals. Move the waypoint transforms to change cable routing. Endpoint anchors are children of the equipment and follow model edits.
- LabInterface: saved dialogue, progression and hint controls.
- LearningSystems: controller and serialized LabSceneView references. Preserve these references when replacing a model.

Equipment prefabs are under Assets/AdaptiveLearning/Prefabs/LabEquipment. They were extracted from the existing Scene 10 equipment; the test wire adapts its existing resistor geometry into a straight conductor. The project had no imported FBX/OBJ equipment assets. These remain the project's assembled 3D models rather than new externally sourced models.

The chatbot is still created on demand by the shared UI integration. Equipment and environment are saved and editable regardless of chat.

Validation: authored prefab instances and perspective camera checked before Play; 14 Play-mode checks passed in a separate project copy; 16:9, 16:10 and 4:3 renders inspected. Unity's isolated editor still emits a SearchDatabase startup exception unrelated to the lesson; no lesson-script exception was found. Physical Android input and live voice acceptance were not repeated in this change. Existing scene-only backups are in Logs/BeforeEditable3DLabs.

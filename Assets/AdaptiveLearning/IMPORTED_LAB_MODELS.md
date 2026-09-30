# Equipment from the supplied G10 archive

Scenes 08 and 09 use the source models from `G10_Ohm's Law_W_VR1.zip`, under `Ohm's Law_W_VROne_G10/Assets/A My Work/Model`.

- DC supply, ammeter, voltmeter and rocker switch: `OhmsLaw_circuit_Final.fbx`.
- Straight test wire and its two clamps: `Ohmslaw_v02.fbx`. The source rod is used to represent the lesson's nichrome wire; mesh geometry alone does not identify a material as nichrome.
- Workbench: `Table.fbx`, including its wood and frame textures.

Editable equipment prefabs are under `Prefabs/ImportedLabEquipment`. Imported source meshes and textures are under `Art/ImportedOhmsLab`; URP materials are in its `URP` subfolder. The archive and original project equipment prefabs are preserved.

In either scene, expand **Equipment - edit models here**, then an instrument and its imported prefab. Mesh transforms and materials can be edited before Play. The table is under **Environment / Imported workbench - editable**. Original placeholder renderers are disabled, retaining existing lesson references and allowing recovery. The original glass meshes are also disabled because their legacy shader obscured the meter faces under URP.

The terminal controls, circuit validation, wire routes, dialogue, chat and lesson flow remain on the existing objects. Move the named wire anchors along with any relocated sockets. The rocker switch has a separate interaction pivot. `LabMeterVisual` maps the imported 0–3 dials to 0.20 A and 1.00 V when the circuit closes; the lesson controller remains responsible for those values and scoring.

Earlier scenes are backed up in `Logs/BeforeImportedLabModels`. Scenes 10 and 11 now also use these imported equipment prefabs; their earlier presentation is backed up in `Logs/BeforeSharedLab`. The shared environment is saved in `Prefabs/Learning/SharedLaboratory.prefab`. Their imported needles are driven by `ImportedExperimentMeters`, with the voltmeter's ×2 range labelled beside its reading. No additional external assets were downloaded. The archive was supplied by the user; redistribution rights for its source models remain unverified.

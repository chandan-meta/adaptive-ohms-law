# Reference styling for Scenes 8 and 9

The supplied image guides the composition, warm workbench, cyan rounded interface, compact terminal controls and dialogue footer. These scenes still use the existing editable equipment meshes; the result is not a pixel-identical reconstruction of the reference's detailed artwork.

Saved UI remains under LabInterface. Terminal controls remain children of their equipment. Each terminal now has a Compact visual child: resize that child to change visible button size without reducing its parent touch target.

The physical camera uses horizontal gate fitting to preserve the tabletop's width at desktop and tablet aspect ratios. Keep its physical camera settings together when reframing. Materials named Reference* control the warm wood, cable colours and accent lighting; BenchWood.png supplies a small procedural grain texture.

LabWireRoute interpolates curved leads between the existing editable route points. Existing circuit validation and progression are unchanged. The styling editor utility is an explicit authoring tool, not a Play-mode rebuild. Do not rerun it on manually edited scenes without a backup, as it adds decorations.

Verified the saved scene changes with 14 Play-mode checks in an isolated Unity project copy and inspected desktop, 16:10 and 4:3 renders. The existing Unity editor SearchDatabase startup exception still occurs in the test copy; no lesson script exception or C# compilation error was found in this run. Physical Android and live voice checks were not repeated for this visual change. Exit Play and reopen Scene 8 or 9 to load the updated authored scene. Scene backups are in Logs/BeforeReferenceStyle.

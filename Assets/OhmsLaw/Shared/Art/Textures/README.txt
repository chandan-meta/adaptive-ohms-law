Repainted copies of the imported instrument textures (Assets/AdaptiveLearning/Art/ImportedOhmsLab/OhmsLaw_circuit_Final.fbm),
so the printed scales match the 0-5 V experiment. Only the numerals changed; arcs, ticks and everything else are the originals.

  VoltMeter_0to6.png    voltmeter dial 0-3 V  -> 0-6 V   (1 V per major division, 0.2 V per minor)
  Ammeter_0to1p2.png    ammeter dial   0-3 A  -> 0-1.2 A (0.2 A per major division, 0.04 A per minor)
  Power_Supply_0to5.jpg battery eliminator panel 2-12 V -> 0-5 V (downscaled to 2048; the lamp emission map stays the original Power_Supply_EMS.jpg)

SceneBuilderBase.Rig.cs copies the URP materials at build time (Shared/Art/Materials/Ammeter_Dial, Voltmeter_Dial, DCSource_Panel)
and points them at these files; CircuitRig drives the needles with fullScale 1.2 A / 6 V over the printed arc.
Made with Pillow (Arial / Arial Bold); the dial arc centre, tick angles and numeral boxes were measured from the originals.

  CableShade.png        8 x 64 greyscale gradient across V: cylinder shading with one highlight band. SceneBuilderBase.Rig.cs
                        puts it on the LeadRed / LeadBlack cable materials (LineRenderer Stretch mode maps V across the ribbon),
                        so the flat cable ribbons read as round tubes. Multiplies _BaseColor.
  RigShadow.png         128 x 128 white disc with alpha 1 inside r 0.35 and a smooth penumbra to the edge: the contact-shadow
                        decal under every rig part (material RigShadow, alpha-blended unlit, drawn before the other transparents).

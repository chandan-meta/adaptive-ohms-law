using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Screen-space callouts (Assets/OhmsLaw/Shared/Callouts.cs). A CalloutDef on the target carries the text; the
    // Timeline puts the panel up with Cue("callout show <id>") and takes it down with Cue("callout hide <id>") or
    // Cue("callout hideall"). Titles and descriptions are the storyboard's label text (ART_DIRECTION section 4):
    // a label, never a number or a result a later scene reveals.
    public abstract partial class SceneBuilderBase
    {
        // Default callout text by the kit's ref name (Lab.cs apparatus, Rig.cs instruments).
        protected static readonly Dictionary<string, (string title, string description)> CalloutTexts = new Dictionary<string, (string, string)>
        {
            ["AMMETER"] = ("Ammeter", "Measures current — I — ampere (A)"),
            ["VOLTMETER"] = ("Voltmeter", "Measures potential difference — V — volt (V)"),
            ["NICHROME WIRE — XY"] = ("Nichrome wire — XY", "Test conductor between X and Y"),
            ["cells / controllable DC source"] = ("Controllable DC source", "Provides a controllable potential difference"),
            ["switch / key"] = ("Switch / key", "Completes or breaks the circuit"),
            ["torsion galvanometer"] = ("Torsion galvanometer", "A needle twists when current flows"),
            ["Mercury Cups"] = ("Mercury cups", "Connection points for the wires"),
            ["Copper Test Wires"] = ("Copper test wires", "Wires of different lengths"),
            ["notebook"] = ("Notebook", "Ohm's handwritten measurements"),
            ["Thermoelectric Source"] = ("Copper–bismuth thermoelectric source", "Provides a steady potential difference"),
        };

        // Concept panel text for Scenes 6 and 12 (ART_DIRECTION section 9: the references' wording would answer the
        // Scene 6 prediction, so the panels define the quantities instead).
        protected const string VoltageConceptTitle = "Voltage (V)", VoltageConceptBody = "The electrical push between two points. Unit: volt (V)";
        protected const string CurrentConceptTitle = "Current (I)", CurrentConceptBody = "Charge flowing past a point each second. Unit: ampere (A)";

        // Object callout on `target`: the marker dot sits at the centre of its renderer bounds (+ offset, metres),
        // the panel at `anchor` (or AboveTarget). The id is what the cues name; one id per scene.
        protected CalloutDef Callout(string id, GameObject target, string title, string description, CalloutDef.Anchor anchor, Vector3 offset = default)
        {
            if (!target) { Debug.LogError($"{SceneId}: callout '{id}' has no target"); return null; }
            var def = Def(id, target);
            def.kind = CalloutDef.Kind.Object;
            def.title = title;
            def.description = description;
            def.anchor = anchor;
            def.offset = offset;
            return def;
        }

        // Same, with the storyboard text for the target's ref name (CalloutTexts). Unknown names get the name alone.
        protected CalloutDef Callout(string id, GameObject target, CalloutDef.Anchor anchor, Vector3 offset = default)
        {
            if (!target) { Debug.LogError($"{SceneId}: callout '{id}' has no target"); return null; }
            if (!CalloutTexts.TryGetValue(target.name, out var text))
            {
                Debug.LogWarning($"{SceneId}: no default callout text for '{target.name}'; pass title and description");
                text = (target.name, "");
            }
            return Callout(id, target, text.title, text.description, anchor, offset);
        }

        // Concept panel: at a screen anchor on its own node, or above `target` when one is given (anchor AboveTarget).
        protected CalloutDef Concept(string id, string title, string body, CalloutDef.Diagram diagram, CalloutDef.Anchor anchor, GameObject target = null)
        {
            var def = Def(id, target ? target : Node("Callout " + id));
            def.kind = CalloutDef.Kind.Concept;
            def.title = title;
            def.description = body;
            def.diagram = diagram;
            def.anchor = anchor;
            return def;
        }

        CalloutDef Def(string id, GameObject host)
        {
            if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CalloutDef>(true)).Any(d => d.id == id))
                Debug.LogWarning($"{SceneId}: callout id '{id}' is defined twice; the cues will pick one of them");
            var def = host.AddComponent<CalloutDef>();
            def.id = id;
            return def;
        }
    }
}

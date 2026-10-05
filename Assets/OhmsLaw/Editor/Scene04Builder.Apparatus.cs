using UnityEngine;
using UnityEngine.SceneManagement;

namespace OhmsLaw.Editor
{
    // Ohm's 1826 apparatus in Scene 4: the shared kit's (SceneBuilderBase.HistoricalApparatus), built on the Victorian
    // desk in the study's own frame, so Scenes 4, 7 and 8 show one apparatus in one layout. Its test wire is Scene 4's own
    // copper wire (same pose, size and polished copper), the one Wattson's beam hits and Scene 5's macro shot copies.
    public static partial class Scene04Builder
    {
        // The kit's builder machinery pointed at Scene 4's scene; only the apparatus is used.
        sealed class ApparatusKit : SceneBuilderBase
        {
            public override string SceneId => "Scene04";
            public override string SceneName => "Scene04_MeetWattson";
            public override string Title => "Meet Wattson & Georg Ohm";
            protected override void Dialogue() { }
            protected override SceneAgentContext Agent() => null;
            protected override void Stage() { }
            protected override void Beats() { }

            public ApparatusKit(Scene target, ModulePalette theme) { scene = target; palette = theme; }
            public Transform Build() => HistoricalApparatus(DeskTop, StudyTurn).transform;
            public Transform this[string name] => refs[name].transform;
        }

        // Builds the apparatus and returns the kit for its refs: "Copper Wire", the Ohm work targets, the needle pivot.
        static ApparatusKit BuildApparatus(Stage s)
        {
            var kit = new ApparatusKit(scene, palette);
            kit.Build();
            s.wire = kit["Copper Wire"];
            return kit;
        }
    }
}

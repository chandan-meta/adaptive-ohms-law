using UnityEngine;

namespace OhmsLaw.Editor
{
    // Framework proof: one line with subtitle and VO, an InteractionGate with an MCQPanel, a branch line, hand-off.
    // Temporary; delete once the real scenes are reviewed.
    public class Scene00TestBuilder : SceneBuilderBase
    {
        public override string SceneId => "Scene00Test";
        public override string SceneName => "Scene00_FrameworkTest";
        public override string Title => "Framework test";

        protected override void Dialogue()
        {
            L("TEST_L01", "This is the blockout framework test. One line, one question, one hand-off.", 4.5f);
            L("TEST_RIGHT", "Correct. The gate resumed the Timeline.", 2.6f, proposed: true);
            L("TEST_WRONG", "Not that one, but the gate still resumed the Timeline.", 3.2f, proposed: true);
            L("TEST_L02", "Test complete.", 1.4f);
        }

        protected override SceneAgentContext Agent() => Context(new[] { "Nothing; this is a test scene." }, new[] { "Everything." },
            ("Is this a test?", "Yes, detective. This scene only checks that my subtitles, voice and questions work."));

        protected override void Stage()
        {
            FloorGrid(Vector3.zero, 10);
            Wattson(new Vector3(0, 1.2f, 0), new Vector3(0, 1.2f, -3));
            Prop(PrimitiveType.Cube, "TEST PROP", null, new Vector3(1.5f, 0.25f, 0), Vector3.one * 0.5f, "A labelled greybox prop");
            Vfx("test effect pulses", null, new Vector3(-1.5f, 1f, 0));
            Cam("Wide", new Vector3(0, 1.6f, -3.5f), new Vector3(0, 1f, 0), 45);
            Cam("Close", new Vector3(0.6f, 1.4f, -1.6f), new Vector3(0, 1.2f, 0), 35);
        }

        protected override void Beats()
        {
            Shot("Wide");
            Gesture(WattsonController.Gesture.Greet);
            Wait(0.8);
            Line("TEST_L01");
            Shot("Close");
            Show("[VFX] test effect pulses");
            Gate("Test_MCQ", "Pick an option; A is correct. autoAnswer picks A after 3 s.", "TEST_RIGHT", "TEST_WRONG");
            Hide("[VFX] test effect pulses");
            Line("TEST_L02");
            Handoff("Scene00TestComplete", cut: false);
        }
    }
}

using UnityEditor;

namespace OhmsLaw.Editor
{
    // Kept for the menu people know. There is one bake for every scene now (BakeBlockoutVO): each line's clip goes into its
    // own scene's Audio/VO. This menu used to bake every line of the module into Scene04/Audio/VO.
    public static class BakeWattsonVO
    {
        [MenuItem("Ohm's Law/Bake Wattson VO")]
        public static void Bake() => BakeBlockoutVO.All();
    }
}

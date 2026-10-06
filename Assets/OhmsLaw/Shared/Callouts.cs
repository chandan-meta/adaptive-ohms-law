using UnityEngine;

namespace OhmsLaw
{
    // Object and concept notes. Builders attach a CalloutDef (id, title, description) to the object; the cues
    // "callout show <id>" / "callout hide <id>" / "callout hideall" put that text in the lesson panel's Info block,
    // one note at a time. Nothing is drawn over the 3D scene any more: the panel is the only instructional surface.
    public static class Callouts
    {
        static string shown = "";

        public static void Show(string id)
        {
            var def = Find(id);
            if (!def) { Debug.LogWarning($"Callout '{id}' has no CalloutDef in the loaded scenes"); return; }
            shown = id;
            if (LessonPanel.Instance) LessonPanel.Instance.Info(id, def.title, def.description);
        }

        public static void Hide(string id)
        {
            if (shown != id) return;
            shown = "";
            if (LessonPanel.Instance) LessonPanel.Instance.InfoClear(id);
        }

        public static void HideAll()
        {
            shown = "";
            if (LessonPanel.Instance) LessonPanel.Instance.InfoClear();
        }

        static CalloutDef Find(string id)
        {
            foreach (var def in Object.FindObjectsByType<CalloutDef>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (def.id == id) return def;
            return null;
        }
    }
}

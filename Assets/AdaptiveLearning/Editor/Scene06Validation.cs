using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class Scene06Validation
    {
        static Scene06Validation()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Scene06Validation",false))
                {
                    new GameObject("Scene06ValidationRunner").AddComponent<Scene06ValidationRunner>();
                }
                if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Scene06Validation",false); StudentProfileManager.ValidationStore = null; }
            };
        }
        [MenuItem("Adaptive Learning/Validate Scene 06")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Scene06Builder.ScenePath);
            SetResolution(1920,1080);
            SessionState.SetBool("Scene06Validation",true); EditorApplication.isPlaying = true;
        }
        // Editor-only Game view control. No runtime dependency on Unity's private editor API.
        public static void SetResolution(int width,int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new object[] { 0 });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var type = assembly.GetType("UnityEditor.GameViewSizeType");
            string label = "Scene06 QA " + width + "x" + height;
            var getCount = group.GetType().GetMethod("GetTotalCount");
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            int count = (int)getCount.Invoke(group,null), index = -1;
            for (int i=0;i<count;i++)
            {
                var size = getSize.Invoke(group,new object[]{i});
                if ((int)sizeType.GetProperty("width").GetValue(size) == width && (int)sizeType.GetProperty("height").GetValue(size) == height) { index=i; break; }
            }
            if (index < 0)
            {
                var size = Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(type,1),width,height,label});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[] { size }); index = count;
            }
            var gameType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameType); window.Show(); window.Focus();
            gameType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(window,index);
        }
    }
}

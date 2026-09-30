using System;
using System.IO;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public interface IStudentProfileStore { StudentLearningProfile Load(); void Save(StudentLearningProfile profile); }
    public sealed class BrowserStudentProfileStore : IStudentProfileStore
    {
        const string Key = "adaptive-learning-profile-v1";
        public StudentLearningProfile Load() => PlayerPrefs.HasKey(Key)
            ? JsonUtility.FromJson<StudentLearningProfile>(PlayerPrefs.GetString(Key)) ?? new StudentLearningProfile()
            : new StudentLearningProfile();
        public void Save(StudentLearningProfile profile)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }
    }
    // Replace this boundary with a cloud store without changing learning components.
    public sealed class JsonStudentProfileStore : IStudentProfileStore
    {
        readonly string path;
        public JsonStudentProfileStore(string path) { this.path = path; }
        public StudentLearningProfile Load()
        {
            if (!File.Exists(path)) return new StudentLearningProfile();
            try { return JsonUtility.FromJson<StudentLearningProfile>(File.ReadAllText(path)) ?? new StudentLearningProfile(); }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                // Preserve malformed data for recovery instead of silently overwriting it.
                File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.Ticks, true);
                Debug.LogWarning("Profile could not be read; preserved a recovery copy. " + e.Message);
                return new StudentLearningProfile();
            }
        }
        public void Save(StudentLearningProfile profile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(profile, true));
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
            else File.Move(path + ".tmp", path);
        }
    }
    public class StudentProfileManager : MonoBehaviour
    {
        public StudentLearningProfile Profile { get; private set; }
        public string SaveError { get; private set; }
        public event Action Changed;
        IStudentProfileStore store;
#if UNITY_EDITOR
        public static IStudentProfileStore ValidationStore;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitializeValidation()
        {
            if (UnityEditor.SessionState.GetBool("ExperimentEvidenceValidation", false))
            { ValidationStore = new MemoryProfileStore(); return; }
            ValidationStore = (UnityEditor.SessionState.GetBool("OutlineReview", false) || UnityEditor.SessionState.GetBool("WarmUIReview", false) || UnityEditor.SessionState.GetBool("ImplementedScenesSmoke", false) || UnityEditor.SessionState.GetBool("Scene12And13Validation", false) || UnityEditor.SessionState.GetBool("LabValidation", false) || UnityEditor.SessionState.GetBool("Scene06Validation", false) || UnityEditor.SessionState.GetBool("Scene07Validation", false) || UnityEditor.SessionState.GetBool("LessonContinuityValidation", false) || UnityEditor.SessionState.GetBool("WattsonChatValidation", false))
                ? new MemoryProfileStore() : null;
        }
#endif
        public void Initialize(float initial, IStudentProfileStore storage = null)
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("CinematicValidation", false))
                storage ??= new MemoryProfileStore();
            storage ??= ValidationStore;
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            store = storage ?? new BrowserStudentProfileStore();
#else
            store = storage ?? new JsonStudentProfileStore(Path.Combine(Application.persistentDataPath, "adaptive-learning-profile.json"));
#endif
            try { Profile = store.Load(); }
            catch (Exception e) { Profile = new StudentLearningProfile(); SaveError = e.Message; Debug.LogWarning("Profile storage unavailable: " + e.Message); }
            Profile.EnsureConcepts(initial);
        }
        public void Save()
        {
            if (Profile == null) return;
            try { store.Save(Profile); SaveError = null; }
            catch (Exception e) { SaveError = e.Message; Debug.LogWarning("Learning progress is in memory; local save failed: " + e.Message); }
            Changed?.Invoke();
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
    }
}






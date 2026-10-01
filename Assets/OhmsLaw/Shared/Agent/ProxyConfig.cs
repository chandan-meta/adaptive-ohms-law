using System;
using UnityEngine;

namespace OhmsLaw
{
    // Where the Wattson backend proxy lives. API keys stay on that server and never enter the Unity build.
    [CreateAssetMenu(menuName = "Ohm's Law/Proxy Config")]
    public class ProxyConfig : ScriptableObject
    {
        [Tooltip("Hosted HTTPS proxy used by builds.")] public string baseUrl = "";
        [Tooltip("Used in the Editor only, for local testing.")] public string editorBaseUrl = "http://localhost:3000";
        [Tooltip("Seconds to wait for the first words before falling back to the offline line.")] public float firstReplyTimeout = 8f;
        [Tooltip("Hard limit for any single request, in seconds.")] public int requestTimeout = 30;
        [Tooltip("Spoken when the proxy cannot be reached.")] public DialogueLine offlineLine;

        public string BaseUrl => (Application.isEditor && !string.IsNullOrEmpty(editorBaseUrl) ? editorBaseUrl : baseUrl).TrimEnd('/');

        // Anonymous per-device id for rate limiting. It is random and carries no personal data.
        public static string SessionId
        {
            get
            {
                string id = PlayerPrefs.GetString("ohm.session", "");
                if (id.Length == 0) PlayerPrefs.SetString("ohm.session", id = Guid.NewGuid().ToString("N"));
                return id;
            }
        }
    }
}

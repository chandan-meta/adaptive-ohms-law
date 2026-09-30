using System;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [CreateAssetMenu(menuName="Adaptive Learning/Wattson service")]
    public class WattsonServiceSettings : ScriptableObject
    {
        [Tooltip("Your application backend endpoint. Provider API keys belong on that server, never in the Unity build.")]
        public string chatEndpoint;
        [Tooltip("Hosted HTTPS backend for tablets. Leave empty to use local lesson responses on Android.")]
        public string androidChatEndpoint;
        public string Endpoint {
            get {
#if UNITY_ANDROID && !UNITY_EDITOR
                return androidChatEndpoint;
#elif UNITY_WEBGL && !UNITY_EDITOR
                return new Uri(new Uri(Application.absoluteURL), "/chat").AbsoluteUri;
#else
                return chatEndpoint;
#endif
            }
        }
        [Range(5,90)] public int timeoutSeconds=30;
        public bool IsConfigured=>!string.IsNullOrWhiteSpace(Endpoint);
        public bool IsValidEndpoint=>Uri.TryCreate(Endpoint,UriKind.Absolute,out var uri)
            &&(uri.Scheme=="https"||(uri.Scheme=="http"&&uri.IsLoopback));
    }
}

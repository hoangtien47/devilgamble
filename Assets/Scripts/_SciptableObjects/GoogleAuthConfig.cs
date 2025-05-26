using UnityEngine;

namespace ZkLogin
{
    [CreateAssetMenu(fileName = "GoogleAuthConfig", menuName = "ZkLogin/Google Auth Config")]
    public class GoogleAuthConfig : ScriptableObject
    {
        [Header("OAuth Configuration")]
        [Tooltip("Your Google OAuth 2.0 Client ID")]
        public string clientId = "YOUR_CLIENT_ID.apps.googleusercontent.com";

        [Header("WebGL Configuration")]
        [Tooltip("URL of your WebGL host for redirect")]
        public string webGLHost = "https://yourgame.com";

        [Tooltip("Path to the auth callback page")]
        public string callbackPath = "/auth-callback.html";

        [Header("Editor Testing")]
        [Tooltip("Port for local testing in editor")]
        public int editorPort = 8080;

        [Header("OAuth Scopes")]
        [Tooltip("Space-separated list of OAuth scopes")]
        public string scopes = "email profile openid";

        public string GetRedirectUri()
        {
#if UNITY_EDITOR
            return $"http://localhost:{editorPort}/oauth2redirect";
#elif UNITY_WEBGL
                return $"{webGLHost}{callbackPath}";
#else
                return $"{webGLHost}{callbackPath}";
#endif
        }
    }
}
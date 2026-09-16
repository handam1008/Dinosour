using UnityEngine;

namespace RYU._01.Script.Auth
{
    [CreateAssetMenu(fileName = "GoogleOAuthConfig", menuName = "RYU/GoogleOAuthConfig", order = 0)]
    public class GoogleOAuthConfig : ScriptableObject
    {
        public string clientId;
        public string clientSecret;
    }
}
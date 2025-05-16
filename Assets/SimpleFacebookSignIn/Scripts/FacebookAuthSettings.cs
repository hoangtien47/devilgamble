using System.Collections.Generic;
using UnityEngine;

namespace Assets.SimpleFacebookSignIn.Scripts
{
    [CreateAssetMenu(fileName = "FacebookAuthSettings", menuName = "Simple Facebook Sign-In/FacebookAuthSettings")]
    public class FacebookAuthSettings : ScriptableObject
    {
        public string Id = "Default";
        public string ClientId;
        public string CustomUriScheme;
        public List<string> AccessScopes = new() { "openid" };
        [Tooltip("Use Safari API on iOS instead of a default web browser. This option is required for passing App Store review.")]
        public bool UseSafariViewController = true;

        public bool Redefined()
        {
            return ClientId != "3542646549392120" && CustomUriScheme != "simple.oauth";
        }
    }
}
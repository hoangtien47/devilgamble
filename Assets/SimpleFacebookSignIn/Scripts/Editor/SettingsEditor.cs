using UnityEditor;
using UnityEngine;

namespace Assets.SimpleFacebookSignIn.Scripts.Editor
{
    [CustomEditor(typeof(FacebookAuthSettings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("- Get `App ID` (alias `Client ID`) in Meta for Developers / My Apps.", MessageType.None);

            DrawDefaultInspector();

            var settings = (FacebookAuthSettings) target;
            
            if (!settings.Redefined())
            {
                EditorGUILayout.HelpBox("Test settings are in use. They are for test purposes only and may be disabled or blocked. Please set your own settings obtained from Meta for Developers.", MessageType.Warning);
            }

            if (GUILayout.Button("Meta for Developers / My Apps"))
            {
                Application.OpenURL("https://developers.facebook.com/apps");
            }

            if (GUILayout.Button("Wiki"))
            {
                Application.OpenURL("https://github.com/hippogamesunity/SimpleFacebookSignIn/wiki");
            }
        }
    }
}
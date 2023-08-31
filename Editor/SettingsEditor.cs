using UnityEditor;

namespace AptabaseSDK
{
    [CustomEditor(typeof(Settings))]
    public class SettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (Settings)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("AppKey"));
            
            if (settings.AppKey.Contains("SH"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SelfHostURL"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AppBuildNumber"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableOverride"));
            if (settings.EnableOverride)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("AppVersion"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FlushInterval"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
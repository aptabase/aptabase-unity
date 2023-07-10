using UnityEditor;

namespace AptabaseSDK
{
    [CustomEditor(typeof(AptabaseSettings))]
    public class AptabaseSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (AptabaseSettings)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("AppKey"));
            
            if (settings.AppKey.Contains("SH"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SelfHostURL"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BuildNumber"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
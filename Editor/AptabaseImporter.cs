using UnityEditor;
using UnityEngine;

namespace AptabaseSDK
{
    /// <summary>
    /// Creates a ScriptableObject representing the Aptabase settings asset in the project's Assets folder.
    /// </summary>
    [InitializeOnLoad]
    internal class AptabaseImporter
    {
        private const string RESOURCE_PATH = "Assets/Aptabase/Resources";
        private const string ASSET_PATH = "AptabaseSettings.asset";
        private const string PATH = RESOURCE_PATH + "/" + ASSET_PATH;
        
        static AptabaseImporter()
        {
            var instance = AssetDatabase.LoadAssetAtPath<AptabaseSettings>(PATH);

            if (instance != null) 
                return;
            
            // Create new instance
            instance = ScriptableObject.CreateInstance<AptabaseSettings>();
            if (!System.IO.Directory.Exists(RESOURCE_PATH))
                System.IO.Directory.CreateDirectory(RESOURCE_PATH);

            AssetDatabase.CreateAsset(instance, PATH);
            AssetDatabase.SaveAssets();
        }
    }
}
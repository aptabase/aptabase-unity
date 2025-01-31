using System;
using UnityEditor;
using UnityEngine;

namespace AptabaseSDK
{
    /// <summary>
    /// Creates a ScriptableObject representing the Aptabase settings asset in the project's Assets folder.
    /// </summary>
    internal class AptabaseImporter : AssetPostprocessor
    {
        private const string RESOURCE_PATH = "Assets/Aptabase/Resources";
        private const string ASSET_PATH = "AptabaseSettings.asset";
        private const string PATH = RESOURCE_PATH + "/" + ASSET_PATH;
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            try
            {
                if (AssetDatabase.FindAssets($"t:{nameof(Settings)}").Length > 0) return;

                //check if file exists
                if (System.IO.File.Exists(PATH))
                    return;
                
                //create needed directories
                if (!System.IO.Directory.Exists(RESOURCE_PATH))
                    System.IO.Directory.CreateDirectory(RESOURCE_PATH);
                
                //create new settings file
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Settings>(), PATH);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError("error creating settings file: " + e.Message);
            }
        }
    }
}
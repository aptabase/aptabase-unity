using UnityEngine;

namespace AptabaseSDK
{
    public class Settings : ScriptableObject
    {
        public string AppKey = "A-EU-0000000000";
        public string SelfHostURL;
        public string AppBuildNumber;
        
        public bool EnableOverride;
        public string AppVersion;
        public int FlushInterval;
    }
}
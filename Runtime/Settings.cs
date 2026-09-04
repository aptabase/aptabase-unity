using UnityEngine;

namespace AptabaseSDK
{
    public class Settings : ScriptableObject
    {
        public string AppKey = "A-EU-0000000000";
        public string SelfHostURL;
        public string AppBuildNumber;

        // Automatically report uncaught exceptions and crashes as error reports
        public bool EnableCrashReporting;
        
        public bool EnableOverride;
        public string AppVersion;
        public int FlushInterval;
    }
}

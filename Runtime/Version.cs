using UnityEngine;

namespace AptabaseSDK
{
    public static class Version
    {
        private const string SDK_VERSION = "Aptabase.Unity@0.2.3";
        
        public static VersionInfo GetVersionInfo(Settings settings)
        {
            return new VersionInfo()
            {
                sdkVersion = SDK_VERSION,
                appVersion = settings.EnableOverride && !string.IsNullOrEmpty(settings.AppVersion) ? settings.AppVersion : Application.version,
                appBuildNumber = settings.AppBuildNumber
            };
        }
    }

    public struct VersionInfo
    {
        public string sdkVersion;
        public string appVersion;
        public string appBuildNumber;
    }
}
using System.Globalization;
using UnityEngine;

namespace AptabaseSDK
{
    public static class Environment
    {
        public static EnvironmentInfo GetEnvironmentInfo(VersionInfo versionInfo)
        {
            return new EnvironmentInfo()
            {
                isDebug = Application.isEditor || Debug.isDebugBuild,
                locale = CultureInfo.CurrentCulture.Name,
                osName = Application.platform.ToString(),
                osVersion = SystemInfo.operatingSystem,
                sdkVersion = versionInfo.sdkVersion,
                appVersion = versionInfo.appVersion,
                appBuildNumber = versionInfo.appBuildNumber
            };
        }
    }

    public struct EnvironmentInfo
    {
        public bool isDebug;
        public string locale;
        public string appVersion;
        public string appBuildNumber;
        public string osName;
        public string osVersion;
        public string sdkVersion;
    }
}
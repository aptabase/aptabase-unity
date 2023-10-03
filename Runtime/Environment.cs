using System.Globalization;
using UnityEngine;

namespace AptabaseSDK
{
    public static class Environment
    {
        public static EnvironmentInfo GetEnvironmentInfo(VersionInfo versionInfo)
        {
            var os = GetOperatingSystemInfo();
            
            return new EnvironmentInfo()
            {
                isDebug = Application.isEditor || Debug.isDebugBuild,
                locale = CultureInfo.CurrentCulture.Name,
                osName = os.osName,
                osVersion = os.osVersion,
                sdkVersion = versionInfo.sdkVersion,
                appVersion = versionInfo.appVersion,
                appBuildNumber = versionInfo.appBuildNumber
            };
        }

        private static OperatingSystemInfo GetOperatingSystemInfo()
        {
            var operatingSystem = new OperatingSystemInfo
            {
                osVersion = SystemInfo.operatingSystem
            };

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    operatingSystem.osName = "Android";
                    var index = operatingSystem.osVersion.IndexOf('(');
                    if (index >= 0)
                    {
                        var trimmedVersion = operatingSystem.osVersion[..index].Trim();
                        operatingSystem.osVersion = trimmedVersion;
                    }
                    break;
                case RuntimePlatform.IPhonePlayer:
                    var model = SystemInfo.deviceModel.ToLower();
                    operatingSystem.osName = model.Contains("ipad") ? "iPadOS" : "iOS";
                    break;
                case RuntimePlatform.LinuxPlayer:
                    operatingSystem.osName = "Linux";
                    break;
                case RuntimePlatform.OSXPlayer:
                    operatingSystem.osName = "macOS";
                    break;
                case RuntimePlatform.WebGLPlayer:
                    operatingSystem.osName = string.Empty;
                    operatingSystem.osVersion = string.Empty;
                    break;
                case RuntimePlatform.WindowsPlayer:
                    operatingSystem.osName = "Windows";
                    break;
                default:
                    operatingSystem.osName = Application.platform.ToString();
                    break;
            }
            return operatingSystem;
        }
    }

    public struct OperatingSystemInfo
    {
        public string osName;
        public string osVersion;
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
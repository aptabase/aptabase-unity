using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public static class Aptabase
    {
        private static string _sessionId = NewSessionId();
        
        private static DateTime _lastTouched = DateTime.UtcNow;
        private static string _baseURL;

        private static readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(60);
        private static readonly Dictionary<string, string> _hosts = new()
        {
            { "US", "https://us.aptabase.com" },
            { "EU", "https://eu.aptabase.com" },
            { "DEV", "http://localhost:3000" },
            { "SH", "" },
        };
        
        private const string EVENT_ENDPOINT = "/api/v0/event";
        private const string SDK_VERSION = "Aptabase.Unity@0.0.1";

        private static AptabaseSettings _settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            //load settings
            _settings = Resources.Load<AptabaseSettings>("AptabaseSettings");
            if (_settings == null)
            {
                Debug.LogWarning("Aptabase Settings not found. Tracking will be disabled");
                return;
            }
            
            var key = _settings.AppKey;
            
            var parts = key.Split("-");
            if (parts.Length != 3 || !_hosts.ContainsKey(parts[1]))
            {
                Debug.LogWarning($"The Aptabase App Key {key} is invalid. Tracking will be disabled");
                return;
            }
            
            _baseURL = GetBaseUrl(parts[1]);
        }
        
        private static string GetBaseUrl(string region)
        {
            if (region == "SH")
            {
                if (string.IsNullOrEmpty(_settings.SelfHostURL))
                {
                    Debug.LogWarning("Host parameter must be defined when using Self-Hosted App Key. Tracking will be disabled.");
                    return null;
                }

                return _settings.SelfHostURL;
            }

            return _hosts[region];
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> props = null)
        {
            SendEvent(eventName, props);
        }

        private static async void SendEvent(string eventName, Dictionary<string, object> props)
        {
            if (string.IsNullOrEmpty(_baseURL))
                return;
            
            try
            {
                var now = DateTime.UtcNow;
                var timeSince = now.Subtract(_lastTouched);
                if (timeSince >= _sessionTimeout)
                    _sessionId = NewSessionId();

                _lastTouched = now;

                props ??= new Dictionary<string, object>();
                
                //create the main dictionary for EventData
                var eventData = Json.Serialize(new Dictionary<string, object>(5)
                {
                    { "timestamp", DateTime.UtcNow.ToString("o") },
                    { "sessionId", _sessionId },
                    { "eventName", eventName },
                    { "systemProps", new Dictionary<string, object>(7)
                    {
                        { "isDebug", Application.isEditor || Debug.isDebugBuild },
                        { "osName", Application.platform.ToString() },
                        { "osVersion", SystemInfo.operatingSystem },
                        { "locale", CultureInfo.CurrentCulture.Name },
                        { "appVersion", Application.version },
                        { "appBuildNumber", _settings.BuildNumber },
                        { "sdkVersion", SDK_VERSION }
                    }},
                    { "props", props }
                });
                
                //send request to end point
                using var www = new UnityWebRequest($"{_baseURL}{EVENT_ENDPOINT}",
                    UnityWebRequest.kHttpVerbPOST);
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("App-Key", _settings.AppKey);
                var requestBytes = Encoding.UTF8.GetBytes(eventData);
                www.uploadHandler = new UploadHandlerRaw(requestBytes);
                www.downloadHandler = new DownloadHandlerBuffer();

                var operation = www.SendWebRequest();

                //wait for complete
                while (!operation.isDone)
                    await Task.Yield();

                //handle results
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to perform TrackEvent due to {www.responseCode} and response body {www.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to perform TrackEvent {e}");
            }
        }
        
        private static string NewSessionId() => Guid.NewGuid().ToString().ToLower();
    }
}
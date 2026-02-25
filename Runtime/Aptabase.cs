using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AptabaseSDK
{
    public static class Aptabase
    {
        private static string _sessionId = NewSessionId();
        private static IDispatcher _dispatcher;
        private static EnvironmentInfo _env;
        private static Settings _settings;
        
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
        
        private static int _flushTimer;
        private static CancellationTokenSource _pollingCancellationTokenSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            //load settings
            _settings = Resources.Load<Settings>("AptabaseSettings");
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

            _env = Environment.GetEnvironmentInfo(Version.GetVersionInfo(_settings));
            
            _baseURL = GetBaseUrl(parts[1]);
            
            #if UNITY_WEBGL
                _dispatcher = new WebGLDispatcher(_settings.AppKey, _baseURL, _env);
            #else
                _dispatcher = new Dispatcher(_settings.AppKey, _baseURL, _env);
            #endif
            
            //create listener
            var eventFocusHandler = new GameObject("AptabaseService");
            eventFocusHandler.AddComponent<AptabaseService>();
        }
        
        private static async void StartPolling(int flushTimer)
        {
            StopPolling();

            _flushTimer = flushTimer;
            _pollingCancellationTokenSource = new CancellationTokenSource();
            
            while (_pollingCancellationTokenSource is { IsCancellationRequested: false })
            {
                try
                {
                    await Task.Delay(_flushTimer, _pollingCancellationTokenSource.Token);
                    Flush();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static void StopPolling()
        {
            if (_flushTimer <= 0)
                return;
            
            _pollingCancellationTokenSource?.Cancel();
            _pollingCancellationTokenSource = null;
            _flushTimer = 0;
        }
        
        public static void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                StartPolling(GetFlushInterval());
            }
            else
            {
                _dispatcher.FlushOrSaveToDisk();
                StopPolling();
            }
        }

        private static string EvalSessionId()
        {
            var now = DateTime.UtcNow;
            var timeSince = now.Subtract(_lastTouched);
            if (timeSince >= _sessionTimeout)
                _sessionId = NewSessionId();

            _lastTouched = now;
            return _sessionId;
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

        public static void Flush()
        {
            _dispatcher.Flush();
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> props = null)
        {
            if (string.IsNullOrEmpty(_baseURL))
                return;
            
            props ??= new Dictionary<string, object>();
            var eventData = new Event()
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                sessionId = EvalSessionId(),
                eventName = eventName,
                systemProps = _env,
                props = props
            };
            
            _dispatcher.Enqueue(eventData);
        }

        private static int GetFlushInterval()
        {
            if (_settings.EnableOverride && _settings.FlushInterval > 0)
                return Mathf.Max(0, _settings.FlushInterval);

            return _env.isDebug ? 2000 : 60000;
        }

        private static string NewSessionId()
        {
            var epochInSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = Random.Range(0, 99999999);
            return (epochInSeconds * 100000000 + random).ToString();
        }
    }
}
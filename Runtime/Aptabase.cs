using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace AptabaseSDK
{
    public static class Aptabase
    {
        private static string _sessionId = NewSessionId();
        private static IDispatcher _dispatcher;
        private static ErrorDispatcher _errorDispatcher;
        private static EnvironmentInfo _env;
        private static EnvironmentInfo _errorEnv;
        private static Settings _settings;

        private static DateTime _lastTouched = DateTime.UtcNow;
        private static string _baseURL;

        private static readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(60);
        // Session state is also touched by crash hooks, which can run off the main thread
        private static readonly object _sessionLock = new();

        private static readonly Dictionary<string, string> _hosts = new()
        {
            { "US", "https://us.aptabase.com" },
            { "EU", "https://eu.aptabase.com" },
            { "DEV", "http://localhost:3000" },
            { "SH", "" }
        };

        private static bool _isEnabled = true;
        private static int _flushTimer;
        private static CancellationTokenSource _pollingCancellationTokenSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // load settings
            _settings = Resources.Load<Settings>("AptabaseSettings");
            if (_settings == null)
            {
                Debug.LogError("[AptabaseAnalytics] Aptabase Settings not found. Tracking will be disabled");
                return;
            }

            var key = _settings.AppKey;

            var parts = key.Split("-");
            if (parts.Length != 3 || !_hosts.ContainsKey(parts[1]))
            {
                Debug.LogError($"[AptabaseAnalytics] The Aptabase App Key {key} is invalid. Tracking will be disabled");
                return;
            }

            _env = Environment.GetEnvironmentInfo(Version.GetVersionInfo(_settings));
            _baseURL = GetBaseUrl(parts[1]);
            if (string.IsNullOrEmpty(_baseURL))
                return;

#if UNITY_WEBGL
            _dispatcher = new WebGLDispatcher(_settings.AppKey, _baseURL, _env);
#else
            _dispatcher = new Dispatcher(_settings.AppKey, _baseURL, _env);
#endif

            // The error endpoint takes one report per request, so a single dispatcher serves all platforms
            _errorEnv = Environment.GetErrorReportingEnvironmentInfo(_env);
            _errorDispatcher = new ErrorDispatcher(_settings.AppKey, _baseURL, _errorEnv, SynchronizationContext.Current);

            if (_settings.EnableCrashReporting)
                CrashReporter.Register();

            // create listener
            var eventFocusHandler = new GameObject("AptabaseService");
            eventFocusHandler.AddComponent<AptabaseService>();
        }

        /// <summary>
        /// Receive the HTTP status code of every event request. Error report requests are not included.
        /// </summary>
        public static void SetResponseListener(Action<HttpStatusCode> onResponse)
        {
            if (_dispatcher == null)
            {
                Debug.LogError("[AptabaseAnalytics] Aptabase is not initialized. Please check your settings.");
                return;
            }

            _dispatcher.SetResponseListener(onResponse);
        }

        /// <summary>
        /// Enables or disables the SDK. Disabling stops polling and drops error reports until re-enabled.
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            if (!enabled)
                StopPolling();
            else
                _ = StartPolling(GetFlushInterval());
        }

        private static async Task StartPolling(int flushTimer)
        {
            StopPolling();

            _flushTimer = flushTimer;
            _pollingCancellationTokenSource = new CancellationTokenSource();

            while (_pollingCancellationTokenSource is { IsCancellationRequested: false })
                try
                {
                    await Task.Delay(_flushTimer, _pollingCancellationTokenSource.Token);
                    await Flush();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
        }

        private static void StopPolling()
        {
            if (_flushTimer <= 0)
                return;

            _pollingCancellationTokenSource?.Cancel();
            _pollingCancellationTokenSource?.Dispose();
            _pollingCancellationTokenSource = null;
            _flushTimer = 0;
        }

        public static void OnApplicationFocus(bool hasFocus)
        {
            if (_isEnabled)
                _ = hasFocus ? StartPolling(GetFlushInterval()) : Flush().ContinueWith(_ => StopPolling());
        }

        private static string EvalSessionId()
        {
            lock (_sessionLock)
            {
                var now = DateTime.UtcNow;
                var timeSince = now.Subtract(_lastTouched);
                if (timeSince >= _sessionTimeout)
                    _sessionId = NewSessionId();

                _lastTouched = now;
                return _sessionId;
            }
        }

        private static string GetBaseUrl(string region)
        {
            if (region == "SH")
            {
                if (string.IsNullOrEmpty(_settings.SelfHostURL))
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Host parameter must be defined when using Self-Hosted App Key. Tracking will be disabled.");
                    return null;
                }

                return _settings.SelfHostURL;
            }

            return _hosts[region];
        }

        /// <summary>
        /// Sends all queued events and error reports. No-op when the SDK failed to initialize.
        /// </summary>
        public static Task Flush(CancellationToken cancellationToken = default)
        {
            if (_dispatcher == null)
                return Task.CompletedTask;

            return Task.WhenAll(_dispatcher.Flush(cancellationToken), _errorDispatcher.Flush(cancellationToken));
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> eventProps = null)
        {
            if (string.IsNullOrEmpty(_baseURL))
                return;

            var props = DictionaryPool<string, object>.Get();
            if (eventProps != null)
                foreach (var prop in eventProps)
                    props.Add(prop.Key, prop.Value);

            var eventData = new Event
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                sessionId = EvalSessionId(),
                eventName = eventName,
                systemProps = _env,
                props = props
            };

            _dispatcher.Enqueue(eventData);
        }

        /// <summary>
        /// Reports an error the app caught and handled. Set <paramref name="fatal"/> for errors the app cannot recover from.
        /// Reports are sent in the background right away; nothing is thrown or awaited. Dropped while the SDK is disabled.
        /// </summary>
        public static void TrackError(Exception exception, bool fatal = false)
        {
            TrackErrorInternal(
                exception,
                fatal ? ErrorSeverity.Fatal : ErrorSeverity.Error,
                fatal ? ErrorKind.Crash : ErrorKind.Handled);
        }

        // Richer entry points used by the crash reporter to convey how the error was captured
        // ("crash", "unhandled", "taskException") without widening the public API
        internal static void TrackErrorInternal(Exception exception, string severity, string kind)
        {
            if (!_isEnabled || _errorDispatcher == null || exception == null)
                return;

            _errorDispatcher.Enqueue(ErrorReport.FromException(exception, severity, kind, EvalSessionId(), _errorEnv));
        }

        internal static void TrackErrorInternal(string condition, string stackTrace, string severity, string kind)
        {
            if (!_isEnabled || _errorDispatcher == null || string.IsNullOrEmpty(condition))
                return;

            _errorDispatcher.Enqueue(ErrorReport.FromLogMessage(condition, stackTrace, severity, kind, EvalSessionId(), _errorEnv));
        }

        private static int GetFlushInterval()
        {
            if (_settings.EnableOverride && _settings.FlushInterval > 0)
                return _settings.FlushInterval;

            return _env.isDebug ? 2000 : 60000;
        }

        private static string NewSessionId()
        {
            // System.Random rather than UnityEngine.Random: a session can roll over from a crash hook running off the main thread
            var epochInSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new System.Random().Next(0, 99999999);
            return (epochInSeconds * 100000000 + random).ToString();
        }
    }
}

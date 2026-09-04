using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AptabaseSDK
{
    /// <summary>
    /// Reports uncaught exceptions automatically when <see cref="Settings.EnableCrashReporting"/> is set.
    ///
    /// Unity catches exceptions thrown from MonoBehaviour callbacks and coroutines and logs them instead of
    /// terminating the app, so those are reported as "unhandled" with severity "error". Only exceptions that
    /// terminate the process (AppDomain.UnhandledException with IsTerminating) are reported as "crash" with
    /// severity "fatal", and delivery of those is best effort. Native (non-managed) crashes are not captured.
    /// </summary>
    internal static class CrashReporter
    {
        private static bool _registered;

        // Guards against re-entrancy: if building or enqueuing a report throws, Unity logs that
        // exception too, which would otherwise call straight back into OnLogMessageReceived
        [ThreadStatic] private static bool _reporting;

        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;

            // The threaded variant also receives exceptions logged from worker threads
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Keeps handlers from piling up across play sessions in the Editor when domain reload is disabled
            Application.quitting += Unregister;
        }

        public static void Unregister()
        {
            if (!_registered)
                return;

            _registered = false;

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            Application.quitting -= Unregister;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Debug.LogError/LogAssertion entries carry no exception type and are too noisy to report
            if (type != LogType.Exception)
                return;

            Report(() => Aptabase.TrackErrorInternal(condition, stackTrace, ErrorSeverity.Error, ErrorKind.Unhandled));
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var severity = e.IsTerminating ? ErrorSeverity.Fatal : ErrorSeverity.Error;
            var kind = e.IsTerminating ? ErrorKind.Crash : ErrorKind.Unhandled;

            Report(() =>
            {
                if (e.ExceptionObject is Exception exception)
                    Aptabase.TrackErrorInternal(exception, severity, kind);
                else
                    Aptabase.TrackErrorInternal(e.ExceptionObject?.ToString(), null, severity, kind);
            });
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Report(() =>
            {
                foreach (var exception in e.Exception.InnerExceptions)
                    Aptabase.TrackErrorInternal(exception, ErrorSeverity.Error, ErrorKind.TaskException);
            });
        }

        private static void Report(Action report)
        {
            if (_reporting)
                return;

            _reporting = true;
            try
            {
                report();
            }
            catch
            {
                // the crash reporter must never surface an exception of its own
            }
            finally
            {
                _reporting = false;
            }
        }
    }
}

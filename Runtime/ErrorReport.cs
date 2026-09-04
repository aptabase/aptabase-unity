using System;

namespace AptabaseSDK
{
    /// <summary>
    /// Severity of a reported error: "fatal" for errors the app cannot recover from, otherwise "error".
    /// </summary>
    public static class ErrorSeverity
    {
        public const string Fatal = "fatal";
        public const string Error = "error";
    }

    /// <summary>
    /// How an error was captured: "handled" (manual TrackError), "unhandled" (caught by the Unity runtime),
    /// "crash" (terminated the process, or reported as fatal) or "taskException" (unobserved Task failure).
    /// </summary>
    public static class ErrorKind
    {
        public const string Crash = "crash";
        public const string Unhandled = "unhandled";
        public const string TaskException = "taskException";
        public const string Handled = "handled";
    }

    /// <summary>
    /// A single error report. Field names match the JSON body of POST /api/v0/error, which takes one report per request.
    /// </summary>
    public struct ErrorReport
    {
        public string errorMessage;
        public string errorType;
        public string stackTrace;
        public string timestamp;
        public string platform;
        public string osName;
        public string osVersion;
        public string appVersion;
        public string sdkVersion;
        public string sessionId;
        public string severity;
        public string kind;
        public bool isDebug;

        // Identifies the SDK platform server-side; deliberately distinct from osName
        private const string PLATFORM = "Unity";

        // Used when a log entry doesn't carry a recognisable exception type
        private const string FALLBACK_ERROR_TYPE = "Exception";

        // Field limits enforced by the server. Exceeding any of them rejects the whole
        // report with 400, so values are truncated at capture time.
        private const int MAX_ERROR_MESSAGE = 5000;
        private const int MAX_ERROR_TYPE = 100;
        private const int MAX_STACK_TRACE = 10000;
        private const int MAX_PLATFORM = 30;
        private const int MAX_OS_NAME = 30;
        private const int MAX_OS_VERSION = 100;
        private const int MAX_APP_VERSION = 50;
        private const int MAX_SDK_VERSION = 40;
        private const int MAX_SESSION_ID = 100;

        public static ErrorReport FromException(Exception exception, string severity, string kind, string sessionId, EnvironmentInfo env)
        {
            return Create(exception.GetType().Name, exception.Message, exception.StackTrace, severity, kind, sessionId, env);
        }

        /// <summary>
        /// Builds a report from a Unity log entry of type <see cref="UnityEngine.LogType.Exception"/>,
        /// whose message is formatted as "ExceptionType: message".
        /// </summary>
        public static ErrorReport FromLogMessage(string condition, string stackTrace, string severity, string kind, string sessionId, EnvironmentInfo env)
        {
            ParseCondition(condition, out var errorType, out var message);
            return Create(errorType, message, stackTrace, severity, kind, sessionId, env);
        }

        // Stamps the report with session/system context at capture time so a report retried on a
        // later flush keeps its original attribution, and clamps every field to the server's limits.
        private static ErrorReport Create(string errorType, string message, string stackTrace, string severity, string kind, string sessionId, EnvironmentInfo env)
        {
            if (string.IsNullOrWhiteSpace(errorType))
                errorType = FALLBACK_ERROR_TYPE;

            var prefix = severity == ErrorSeverity.Fatal ? "Fatal " : string.Empty;

            return new ErrorReport
            {
                errorMessage = Truncate($"{prefix}{errorType}: {message}", MAX_ERROR_MESSAGE),
                errorType = Truncate(errorType, MAX_ERROR_TYPE),
                stackTrace = string.IsNullOrEmpty(stackTrace) ? null : Truncate(stackTrace, MAX_STACK_TRACE),
                timestamp = DateTime.UtcNow.ToString("o"),
                platform = Truncate(PLATFORM, MAX_PLATFORM),
                osName = Truncate(env.osName, MAX_OS_NAME),
                osVersion = Truncate(env.osVersion, MAX_OS_VERSION),
                appVersion = Truncate(env.appVersion, MAX_APP_VERSION),
                sdkVersion = Truncate(env.sdkVersion, MAX_SDK_VERSION),
                sessionId = Truncate(sessionId, MAX_SESSION_ID),
                severity = severity,
                kind = kind,
                isDebug = env.isDebug
            };
        }

        // Unity formats exception log entries as "ExceptionType: message". Anything that doesn't
        // look like that is reported verbatim under the fallback type.
        private static void ParseCondition(string condition, out string errorType, out string message)
        {
            condition ??= string.Empty;

            var separator = condition.IndexOf(": ", StringComparison.Ordinal);
            var head = separator > 0 ? condition[..separator] : condition;

            if (LooksLikeTypeName(head))
            {
                errorType = ShortTypeName(head);
                message = separator > 0 ? condition[(separator + 2)..] : string.Empty;
            }
            else
            {
                errorType = FALLBACK_ERROR_TYPE;
                message = condition;
            }
        }

        private static bool LooksLikeTypeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var c in value)
            {
                // Allow namespaces (System.IO.IOException), nested types (Outer+Inner) and generics (Foo`1)
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '+' && c != '`')
                    return false;
            }

            return true;
        }

        // Matches Exception.GetType().Name, which is what TrackError(Exception) reports
        private static string ShortTypeName(string typeName)
        {
            var lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value != null && value.Length > maxLength ? value[..maxLength] : value;
        }
    }
}

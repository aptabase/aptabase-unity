using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.TinyJson;
using UnityEngine;

namespace AptabaseSDK
{
    // Errors have their own dispatcher because the error endpoint takes a single report per
    // request (so WebGL and the other platforms share this class, unlike events) and has
    // different retry semantics than the events endpoint: 408/429/5xx are retried, while
    // 403 (monthly error quota exhausted) must never be retried.
    public class ErrorDispatcher
    {
        private const string ERROR_ENDPOINT = "/api/v0/error";

        // Max number of unsent reports kept in memory; new reports are dropped when full,
        // so the first occurrences (closest to the root cause) are kept
        private const int MAX_QUEUE_SIZE = 25;

        // Only the first occurrence of each unique error is reported per session. Unity keeps
        // running after an exception, so a throwing Update() would otherwise produce a report
        // every frame. The cap bounds both memory and quota usage in a runaway session.
        private const int MAX_UNIQUE_ERRORS_PER_SESSION = 100;

        private readonly WebRequestHelper _webRequestHelper;

        // UnityWebRequest can only be used from the main thread, but crash hooks can fire from
        // any thread, so off-thread enqueues post their flush back to the main thread
        private readonly int _mainThreadId;
        private readonly SynchronizationContext _mainThreadContext;

        private readonly object _lock = new();
        private readonly Queue<ErrorReport> _reports = new();
        private readonly HashSet<ulong> _seen = new();
        private string _seenSessionId;
        private bool _sessionCapReached;
        private Task _pendingFlush = Task.CompletedTask;

        public ErrorDispatcher(string appKey, string baseURL, EnvironmentInfo env, SynchronizationContext mainThreadContext)
        {
            _webRequestHelper = new WebRequestHelper($"{baseURL}{ERROR_ENDPOINT}", appKey, env);
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _mainThreadContext = mainThreadContext;
        }

        // Safe to call from any thread
        public void Enqueue(ErrorReport report)
        {
            bool firstOfBatch;

            lock (_lock)
            {
                if (!ShouldReport(report))
                    return;

                if (_reports.Count >= MAX_QUEUE_SIZE)
                {
                    Debug.LogWarning("[AptabaseAnalytics] Error report queue is full. Dropping report.");
                    return;
                }

                _reports.Enqueue(report);

                // Only the first report of a batch kicks a flush: the flush drains the whole
                // queue, so reports enqueued while it is pending ride along
                firstOfBatch = _reports.Count == 1;
            }

            if (!firstOfBatch)
                return;

            // Errors are high-value: attempt delivery right away instead of waiting for the
            // next poll tick; failures stay queued for the regular flushes
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                _ = Flush();
            else
                _mainThreadContext?.Post(state => _ = Flush(), null);
            // With no context to hop back to, the next periodic flush delivers the report
        }

        // Flushes are chained so one requested while another is in progress runs after it and
        // retries whatever that one re-queued, instead of being skipped: awaiting Flush() means
        // every report queued at the time of the call has been attempted. Must be called from
        // the main thread.
        public Task Flush(CancellationToken cancellationToken = default)
        {
            _pendingFlush = FlushAfter(_pendingFlush, cancellationToken);
            return _pendingFlush;
        }

        private async Task FlushAfter(Task previous, CancellationToken cancellationToken)
        {
            // FlushQueue never throws, so the previous flush can't fault
            await previous;
            await FlushQueue(cancellationToken);
        }

        private async Task FlushQueue(CancellationToken cancellationToken)
        {
            var failed = new List<ErrorReport>();

            while (!cancellationToken.IsCancellationRequested && TryDequeue(out var report))
            {
                var settled = false;
                try
                {
                    settled = await Send(report, cancellationToken);
                }
                catch (Exception)
                {
                    // treated like a network failure: keep the report for a later flush
                }

                if (!settled)
                    failed.Add(report);
            }

            if (failed.Count > 0)
                Requeue(failed);
        }

        // Returns true when the report is settled (delivered or dropped) and
        // false when it should be kept for a retry on a later flush
        private async Task<bool> Send(ErrorReport report, CancellationToken cancellationToken)
        {
            var result = await _webRequestHelper.CreateAndSendWebRequestWithResultAsync(report.ToJson(), cancellationToken);

            if (result.success)
                return true;

            if (result.cancelled)
                return false;

            var reason = $"{result.statusCode} {(string.IsNullOrEmpty(result.responseBody) ? result.error : result.responseBody)}";

            // The server reports an exhausted monthly error quota as 403 (not 429)
            // precisely so clients drop the report instead of retrying it
            if (result.statusCode == 403)
            {
                Debug.LogWarning($"[AptabaseAnalytics] Error report rejected because of {reason}. Will not retry.");
                return true;
            }

            if (result.isConnectionError || result.statusCode == 408 || result.statusCode == 429 || result.statusCode >= 500)
            {
                Debug.LogWarning($"[AptabaseAnalytics] Failed to send error report because of {reason}. Will retry later.");
                return false;
            }

            Debug.LogWarning($"[AptabaseAnalytics] Failed to send error report because of {reason}. Will not retry.");
            return true;
        }

        private bool TryDequeue(out ErrorReport report)
        {
            lock (_lock)
            {
                if (_reports.Count > 0)
                {
                    report = _reports.Dequeue();
                    return true;
                }

                report = default;
                return false;
            }
        }

        // Failed reports go back to the front so retries preserve the original order
        private void Requeue(List<ErrorReport> failed)
        {
            lock (_lock)
            {
                var pending = new List<ErrorReport>(_reports);
                _reports.Clear();

                foreach (var report in failed)
                    _reports.Enqueue(report);

                foreach (var report in pending)
                {
                    if (_reports.Count >= MAX_QUEUE_SIZE)
                    {
                        Debug.LogWarning("[AptabaseAnalytics] Error report queue is full. Dropping report.");
                        break;
                    }

                    _reports.Enqueue(report);
                }
            }
        }

        // Called under _lock
        private bool ShouldReport(ErrorReport report)
        {
            if (report.sessionId != _seenSessionId)
            {
                _seen.Clear();
                _seenSessionId = report.sessionId;
                _sessionCapReached = false;
            }

            if (_sessionCapReached)
                return false;

            // Already reported in this session
            if (!_seen.Add(ComputeKey(report)))
                return false;

            if (_seen.Count >= MAX_UNIQUE_ERRORS_PER_SESSION)
            {
                _sessionCapReached = true;
                Debug.LogWarning($"[AptabaseAnalytics] {MAX_UNIQUE_ERRORS_PER_SESSION} unique errors reported this session. Further errors will not be reported until a new session starts.");
            }

            return true;
        }

        // FNV-1a over the fields that identify an error, so the per-session dedupe set stays
        // small even though stack traces can be several KB each
        private static ulong ComputeKey(ErrorReport report)
        {
            const ulong offsetBasis = 14695981039346656037;
            var hash = offsetBasis;

            Mix(ref hash, report.kind);
            Mix(ref hash, report.errorType);
            Mix(ref hash, report.errorMessage);
            Mix(ref hash, report.stackTrace);

            return hash;
        }

        private static void Mix(ref ulong hash, string value)
        {
            const ulong prime = 1099511628211;

            unchecked
            {
                if (value != null)
                {
                    foreach (var c in value)
                    {
                        hash ^= c;
                        hash *= prime;
                    }
                }

                // field separator, so ("ab", "c") and ("a", "bc") don't collide
                hash ^= '\n';
                hash *= prime;
            }
        }
    }
}

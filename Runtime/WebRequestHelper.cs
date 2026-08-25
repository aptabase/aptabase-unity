using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public struct WebRequestResult
    {
        public bool success;
        // True when the request was abandoned because the CancellationToken fired
        public bool cancelled;
        public UnityWebRequest.Result result;
        // 0 when the request never reached the server
        public long statusCode;
        public string error;
        public string responseBody;

        // No response at all (offline, DNS, TLS, timeout)
        public bool isConnectionError => result == UnityWebRequest.Result.ConnectionError;
    }

    // Accepts any certificate. Only ever attached to requests targeting a loopback address over
    // https, so a local Aptabase instance served with a self-signed development certificate can be reached.
    internal class LocalCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public class WebRequestHelper
    {
        private readonly string _appKey;
        private readonly string _url;
        private readonly string _userAgent;
        private readonly bool _trustLocalCertificate;
        private Action<HttpStatusCode> _onResponse;

        public WebRequestHelper(string url, string appKey, EnvironmentInfo env)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("[AptabaseAnalytics] URL cannot be null or empty", nameof(url));

            if (string.IsNullOrEmpty(appKey))
                throw new ArgumentException("[AptabaseAnalytics] AppKey cannot be null or empty", nameof(appKey));

            _url = url;
            _appKey = appKey;
            _userAgent = $"{env.osName}/{env.osVersion} {env.locale}";
            _trustLocalCertificate = IsLocalHttps(url);
        }

        // The local backend's dev certificate is self-signed. Matched on the parsed host rather than a
        // string prefix so that e.g. https://localhost.example.com never has verification disabled.
        private static bool IsLocalHttps(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && uri.Scheme == Uri.UriSchemeHttps
                   && uri.IsLoopback;
        }

        public async Task<bool> CreateAndSendWebRequestAsync(string contents, CancellationToken cancellationToken)
        {
            return await SendWebRequestAsync(CreateWebRequest(contents), cancellationToken);
        }

        // Like CreateAndSendWebRequestAsync but leaves logging to the caller and returns the status code,
        // for callers whose retry decision depends on it
        public async Task<WebRequestResult> CreateAndSendWebRequestWithResultAsync(string contents, CancellationToken cancellationToken)
        {
            return await SendWebRequestWithResultAsync(CreateWebRequest(contents), cancellationToken);
        }

        private UnityWebRequest CreateWebRequest(string contents)
        {
            var webRequest = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("App-Key", _appKey);
            // webgl needs the default user-agent header. All other platforms we create manually
#if !UNITY_WEBGL
            webRequest.SetRequestHeader("User-Agent", _userAgent);

            // Certificate handlers are not supported on WebGL, where the browser validates TLS
            // (trust the dev certificate in the browser instead). Disposed together with the request.
            if (_trustLocalCertificate)
                webRequest.certificateHandler = new LocalCertificateHandler();
#endif

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));
            return webRequest;
        }

        private async Task<bool> SendWebRequestAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken)
        {
            var result = await SendWebRequestWithResultAsync(request, cancellationToken);
            if (result.cancelled)
                return false;

            if (!result.success)
                Debug.LogWarning(
                    $"[AptabaseAnalytics] Failed to perform web request due to {result.statusCode} " +
                    $"and response body {result.error}, " +
                    $"result: {result.result}.");

            return result.success;
        }

        private async Task<WebRequestResult> SendWebRequestWithResultAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken)
        {
            var requestOp = request.SendWebRequest();
            while (!requestOp.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Abort so the server doesn't process a request the caller is going to retry anyway
                    request.Abort();
                    request.Dispose();
                    return new WebRequestResult { cancelled = true };
                }

                await Task.Yield();
            }

            var webRequest = requestOp.webRequest;
            var result = new WebRequestResult
            {
                success = webRequest.result is UnityWebRequest.Result.Success,
                result = webRequest.result,
                statusCode = webRequest.responseCode,
                error = webRequest.error,
                responseBody = webRequest.downloadHandler?.text
            };

            try
            {
                // Invoke the user's (optional) callback with the response code
                _onResponse?.Invoke((HttpStatusCode)webRequest.responseCode);
            }
            catch (Exception ex)
            {
                // Ignore any exceptions thrown by the callback to avoid crashing the application
                Debug.LogException(ex);
            }
            finally
            {
                request.Dispose();
            }

            return result;
        }

        public void SetResponseListener(Action<HttpStatusCode> onResponse)
        {
            _onResponse = onResponse;
        }
    }
}

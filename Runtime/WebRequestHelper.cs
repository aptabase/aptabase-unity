using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public class WebRequestHelper
    {
        private readonly string _appKey;
        private readonly string _url;
        private readonly string _userAgent;
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
        }

        public async Task<bool> CreateAndSendWebRequestAsync(string contents, CancellationToken cancellationToken)
        {
            return await SendWebRequestAsync(CreateWebRequest(contents), cancellationToken);
        }

        private UnityWebRequest CreateWebRequest(string contents)
        {
            var webRequest = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("App-Key", _appKey);
            // webgl needs the default user-agent header. All other platforms we create manually
#if !UNITY_WEBGL
            webRequest.SetRequestHeader("User-Agent", _userAgent);
#endif

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));
            return webRequest;
        }

        private async Task<bool> SendWebRequestAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken)
        {
            var requestOp = request.SendWebRequest();
            while (!requestOp.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                await Task.Yield();
            }

            var success = requestOp.webRequest.result is UnityWebRequest.Result.Success;
            if (!success)
                Debug.LogWarning(
                    $"[AptabaseAnalytics] Failed to perform web request due to {requestOp.webRequest.responseCode} " +
                    $"and response body {requestOp.webRequest.error}, " +
                    $"result: {requestOp.webRequest.result}.");

            try
            {
                // Invoke the user's (optional) callback with the response code
                _onResponse?.Invoke((HttpStatusCode)requestOp.webRequest.responseCode);
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

            return success;
        }

        public void SetResponseListener(Action<HttpStatusCode> onResponse)
        {
            _onResponse = onResponse;
        }
    }
}
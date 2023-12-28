using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public class WebRequestHelper
    {
        public UnityWebRequest CreateWebRequest(string url, string appKey, EnvironmentInfo env, string contents)
        {
            var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("App-Key", appKey);
//webgl needs the default user-agent header. All other platforms we create manually
#if !UNITY_WEBGL
            webRequest.SetRequestHeader("User-Agent", $"{env.osName}/${env.osVersion} ${env.locale}");
#endif
                
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));
            return webRequest;
        }
        
        public async Task<bool> SendWebRequestAsync(UnityWebRequest request)
        {
            var requestOp = request.SendWebRequest();
            while (!requestOp.isDone)
                await Task.Yield();

            var success = requestOp.webRequest.result == UnityWebRequest.Result.Success;
            if (success)
            {
                request.Dispose();
            }
            else
            {
                Debug.LogWarning($"Failed to perform web request due to {requestOp.webRequest.responseCode} and response body {requestOp.webRequest.error}");
                request.Dispose();
            }

            return success;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AptabaseSDK.TinyJson;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public class Dispatcher
    {
        private const string EVENTS_ENDPOINT = "/api/v0/events";
        
        private const int MAX_BATCH_SIZE = 25;
        
        private readonly Queue<Event> _events;
        private static string _apiURL;
        private static Dictionary<string, string> _headers;
        private bool _flushInProgress;
        
        public Dispatcher(string appKey, string baseURL, EnvironmentInfo env)
        {
            //create event queue
            _events = new Queue<Event>();
            
            //setup web request
            _apiURL = $"{baseURL}{EVENTS_ENDPOINT}";
            _headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "App-Key", appKey },
                { "User-Agent", $"{env.osName}/${env.osVersion} ${env.locale}"}
            };
        }
        
        public void Enqueue(Event data)
        {
            _events.Enqueue(data);
        }
        
        private void Enqueue(List<Event> data)
        {
            foreach (var eventData in data)
                _events.Enqueue(eventData);
        }

        public async void Flush()
        {
            if (_flushInProgress || _events.Count <= 0)
                return;

            _flushInProgress = true;
            var failedEvents = new List<Event>();
            
            //flush all events
            do
            {
                var eventsCount = Mathf.Min(MAX_BATCH_SIZE, _events.Count);
                var eventsToSend = new List<Event>();
                for (var i = 0; i < eventsCount; i++)
                    eventsToSend.Add(_events.Dequeue());

                try
                {
                    await SendEvents(eventsToSend);
                }
                catch (Exception e)
                {
                    failedEvents.AddRange(eventsToSend);
                    Debug.LogError(e);
                }

            } while (_events.Count > 0);
            
            if (failedEvents.Count > 0) 
            { 
                Enqueue(failedEvents);
            }

            _flushInProgress = false;
        }
        
        private static async Task SendEvents(List<Event> events)
        {
            try
            {
                var webRequest = new UnityWebRequest(_apiURL, UnityWebRequest.kHttpVerbPOST);
                foreach (var header in _headers)
                    webRequest.SetRequestHeader(header.Key, header.Value);
                
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(events.ToJson()));
                var sendOperation = webRequest.SendWebRequest();

                //wait for complete
                while (!sendOperation.isDone)
                    await Task.Yield();

                //handle results
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        $"Failed to perform TrackEvent due to {webRequest.responseCode} and response body {webRequest.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to perform TrackEvent {e}");
            }
        }
    }
}
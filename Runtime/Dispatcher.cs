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
        private const string EVENT_ENDPOINT = "/api/v0/event";
        private const string EVENTS_ENDPOINT = "/api/v0/events";
        private const int MAX_BATCH_SIZE = 25;
        
        private readonly Queue<Event> _events;
        private static UnityWebRequest _webRequest;
        
        public Dispatcher(string appKey, string baseURL, EnvironmentInfo env)
        {
            //create event queue
            _events = new Queue<Event>();
            
            //setup web request
            _webRequest = new UnityWebRequest($"{baseURL}{EVENTS_ENDPOINT}", UnityWebRequest.kHttpVerbPOST);
            _webRequest.SetRequestHeader("Content-Type", "application/json");
            _webRequest.SetRequestHeader("App-Key", appKey);
            _webRequest.SetRequestHeader("User-Agent", $"{env.osName}/${env.osVersion} ${env.locale}");
            _webRequest.downloadHandler = new DownloadHandlerBuffer();
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
            if (_events.Count <= 0)
                return;

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
        }
        
        private static async Task SendEvents(List<Event> events)
        {
            try
            {
                _webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(events.ToJson()));
                var operation = _webRequest.SendWebRequest();

                //wait for complete
                while (!operation.isDone)
                    await Task.Yield();

                //handle results
                if (_webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to perform TrackEvent due to {_webRequest.responseCode} and response body {_webRequest.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to perform TrackEvent {e}");
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AptabaseSDK.TinyJson;
using UnityEngine;

namespace AptabaseSDK
{
    public class Dispatcher: IDispatcher
    {
        private const string EVENTS_ENDPOINT = "/api/v0/events";
        private const string APTABASE_KEY = "aptabase_key";
        
        private const int MAX_BATCH_SIZE = 25;
        
        private static string _apiURL;
        private static WebRequestHelper _webRequestHelper;
        private static string _appKey;
        private static EnvironmentInfo _environment;
        
        private bool _flushInProgress;
        private readonly Queue<Event> _events;
        
        public Dispatcher(string appKey, string baseURL, EnvironmentInfo env)
        {
            var cachedEventsJson = PlayerPrefs.GetString(APTABASE_KEY);
            var cacheEvents = string.IsNullOrEmpty(cachedEventsJson) ? new List<Event>() : cachedEventsJson.FromJson<List<Event>>();

            //create event queue
            _events = new Queue<Event>(cacheEvents);
            
            //web request setup information
            _apiURL = $"{baseURL}{EVENTS_ENDPOINT}";
            _appKey = appKey;
            _environment = env;
            _webRequestHelper = new WebRequestHelper();

            PlayerPrefs.DeleteKey(APTABASE_KEY);
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

        public async Task Flush()
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
                    var result = await SendEvents(eventsToSend);
                    if (!result) failedEvents.AddRange(eventsToSend);
                }
                catch
                {
                    failedEvents.AddRange(eventsToSend);
                }

            } while (_events.Count > 0);
            
            if (failedEvents.Count > 0) 
                Enqueue(failedEvents);

            _flushInProgress = false;
        }

        public async Task FlushOrSaveToDisk()
        {
            await Flush();
            
            PlayerPrefs.SetString(APTABASE_KEY, _events.Take(1000).ToList().ToJson());
        }

        private static async Task<bool> SendEvents(List<Event> events)
        {
            if(Application.internetReachability == NetworkReachability.NotReachable) return false;
            
            var webRequest = _webRequestHelper.CreateWebRequest(_apiURL, _appKey, _environment, events.ToJson());
            var result = await _webRequestHelper.SendWebRequestAsync(webRequest);
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.TinyJson;

namespace AptabaseSDK
{
    public class WebGLDispatcher : IDispatcher
    {
        private const string EVENT_ENDPOINT = "/api/v0/event";
        private readonly Queue<Event> _events;

        private readonly WebRequestHelper _webRequestHelper;

        private bool _flushInProgress;

        public WebGLDispatcher(string appKey, string baseURL, EnvironmentInfo env)
        {
            // create the event queue
            _events = new Queue<Event>();

            // web request setup information
            _webRequestHelper = new WebRequestHelper($"{baseURL}{EVENT_ENDPOINT}", appKey, env);
        }

        public void SetResponseListener(Action<HttpStatusCode> onResponse)
        {
            _webRequestHelper.SetResponseListener(onResponse);
        }

        public void Enqueue(Event data)
        {
            _events.Enqueue(data);
            _ = Flush();
        }

        public async Task Flush(CancellationToken cancellationToken = default)
        {
            if (_flushInProgress || _events.Count <= 0)
                return;

            _flushInProgress = true;
            var failedEvents = new List<Event>();

            // flush all events
            do
            {
                var eventToSend = _events.Dequeue();
                try
                {
                    var result = await SendEvent(eventToSend, cancellationToken);
                    if (!result) failedEvents.Add(eventToSend);
                }
                catch
                {
                    failedEvents.Add(eventToSend);
                }
            } while (_events.Count > 0 && !cancellationToken.IsCancellationRequested);

            if (failedEvents.Count > 0)
                Enqueue(failedEvents);

            _flushInProgress = false;
        }

        private void Enqueue(List<Event> data)
        {
            foreach (var eventData in data)
                _events.Enqueue(eventData);
        }

        private async Task<bool> SendEvent(Event eventData, CancellationToken cancellationToken)
        {
            return await _webRequestHelper.CreateAndSendWebRequestAsync(eventData.ToJson(), cancellationToken);
        }
    }
}
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AptabaseSDK
{
    public interface IDispatcher
    {
        void SetResponseListener(Action<HttpStatusCode> onResponse);
        
        void Enqueue(Event data);

        Task Flush(CancellationToken cancellationToken);
    }
}
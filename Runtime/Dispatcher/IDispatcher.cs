using System.Threading.Tasks;

namespace AptabaseSDK
{
    public interface IDispatcher
    {
        void Enqueue(Event data);

        Task Flush();
        
        Task FlushOrSaveToDisk();
    }
}
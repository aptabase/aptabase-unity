namespace AptabaseSDK
{
    public interface IDispatcher
    {
        public void Enqueue(Event data);

        public void Flush();
    }
}
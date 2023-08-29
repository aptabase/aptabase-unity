using System.Collections.Generic;

namespace AptabaseSDK
{
    public struct Event
    {
        public string timestamp;
        public string sessionId;
        public string eventName;
        public EnvironmentInfo systemProps;
        public Dictionary<string, object> props;
    }
}
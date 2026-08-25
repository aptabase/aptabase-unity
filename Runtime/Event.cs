using System;
using System.Collections.Generic;

namespace AptabaseSDK
{
    public struct Event : IEquatable<Event>
    {
        public string timestamp;
        public string sessionId;
        public string eventName;
        public EnvironmentInfo systemProps;
        public Dictionary<string, object> props;

        public bool Equals(Event other)
        {
            return timestamp == other.timestamp
                   && sessionId == other.sessionId
                   && eventName == other.eventName
                   && systemProps == other.systemProps
                   && Equals(props, other.props);
        }

        public override bool Equals(object obj)
        {
            return obj is Event other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(timestamp, sessionId, eventName, systemProps, props);
        }

        public static bool operator ==(Event left, Event right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Event left, Event right)
        {
            return !left.Equals(right);
        }
    }
}
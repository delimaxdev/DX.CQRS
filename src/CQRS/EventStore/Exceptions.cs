using System;
using System.Runtime.Serialization;

namespace DX.Cqrs.EventStore {
    [Serializable]
    public class EventStoreConfigurationException : Exception {
        public EventStoreConfigurationException() { }
        public EventStoreConfigurationException(string message) : base(message) { }
        protected EventStoreConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class EventStoreException : Exception {
        public EventStoreException() { }
        public EventStoreException(string message) : base(message) { }
        protected EventStoreException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

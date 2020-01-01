using DX.Contracts;
using DX.Cqrs.EventStore;
using DX.Testing;
using System;
using Xbehave;

namespace EventStore {
    public class EventStoreFeature : Feature {
        [Scenario]
        public void Stream() {
            CUSTOM["Creating an EventBatch with events that have a different StreamID than the EventBatch", ThrowsA<ArgumentException>()] = () =>
                new EventBatch<Customer>(1, new[] { new RecordedEvent(streamID: 2, new Customer.Created(), new object()) });
        }

        private class Customer {
            public class Created : IEvent { }
        }
    }
}

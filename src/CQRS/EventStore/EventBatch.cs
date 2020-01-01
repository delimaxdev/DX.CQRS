using System.Collections.Generic;
using System.Linq;

namespace DX.Cqrs.EventStore {
    public class EventBatch {
        public EventBatch(object streamID, IReadOnlyCollection<RecordedEvent> events) {
            Check.NotNull(streamID, nameof(streamID));

            Check.Requires(
                events.All(x => streamID.Equals(x.StreamID)),
                nameof(events), "All events must have the same StreamID as the Stream.");

            (StreamID, Events) = (streamID, events);
        }

        public object StreamID { get; }

        public IReadOnlyCollection<RecordedEvent> Events { get; }

        public override string ToString() {
            return $"EventBatch({Events.Count} events)";
        }
    }

    public class EventBatch<T> : EventBatch {
        public EventBatch(object streamID, IReadOnlyCollection<RecordedEvent> events)
            : base(streamID, events) { }
    }
}

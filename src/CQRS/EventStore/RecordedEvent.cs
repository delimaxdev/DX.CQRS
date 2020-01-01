namespace DX.Cqrs.EventStore
{
    public class RecordedEvent {
        public object? ID { get; set; }

        public object StreamID { get; }

        public object Event { get; }
        
        public object Metadata { get; }

        public RecordedEvent(object streamID, object @event, object metadata) {
            StreamID = Check.NotNull(streamID, nameof(streamID));
            Event = Check.NotNull(@event, nameof(@event));
            Metadata = Check.NotNull(metadata, nameof(metadata));
        }
    }
}

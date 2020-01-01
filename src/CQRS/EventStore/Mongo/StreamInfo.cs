namespace DX.Cqrs.EventStore.Mongo
{
    internal sealed class StreamInfo {
        public object StreamID { get; }

        public StreamInfo(object streamID) {
            Check.NotNull(streamID, nameof(streamID));
            StreamID = streamID;
        }
    }
}

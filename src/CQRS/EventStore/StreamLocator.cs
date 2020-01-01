namespace DX.Cqrs.EventStore {
    public class StreamLocator<T> {
        public StreamLocator(object streamID) {
            Check.NotNull(streamID, nameof(streamID));
            StreamID = streamID;
        }

        public object StreamID { get; }
    }
}

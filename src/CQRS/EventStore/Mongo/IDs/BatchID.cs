using System;

namespace DX.Cqrs.EventStore.Mongo {
    internal struct BatchID : IEquatable<BatchID> {
        public static readonly BatchID Empty = new BatchID();

        public DateTime Timestamp { get; private set; }

        public int Sequence { get; private set; }

        public BatchID(DateTime timestamp) {
            EventID.ValidateTimestamp(timestamp);

            Timestamp = EventID.GetTimestampDate(timestamp);
            Sequence = 1;
        }

        public bool TryToAdvance(DateTime timestamp) {
            Check.Requires<InvalidOperationException>(timestamp > Timestamp);

            timestamp = EventID.GetTimestampDate(timestamp);

            if (Timestamp == timestamp) {
                if (Sequence >= EventID.MaxBatchSequence)
                    return false;

                Sequence++;
            } else {
                this = new BatchID(timestamp);
            }

            return true;
        }

        public bool Equals(BatchID other)
            => Timestamp == other.Timestamp && Sequence == other.Sequence;

        public override bool Equals(object other) {
            if (other is BatchID otherID)
                return Equals(otherID);

            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(Timestamp, Sequence);

        public override string ToString()
            => $"{Timestamp:yyyy-MM-dd HH:mm:ss.ff}, {Sequence:D3}";

        public static bool operator ==(BatchID left, BatchID right)
            => left.Equals(right);

        public static bool operator !=(BatchID left, BatchID right)
            => !(left == right);

        public static BatchID DeserializeInternal(DateTime timestamp, int sequence)
            => new BatchID { Timestamp = timestamp, Sequence = sequence };
    }
}

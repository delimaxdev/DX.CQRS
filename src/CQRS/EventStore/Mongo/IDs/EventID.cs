using System;

namespace DX.Cqrs.EventStore.Mongo {
    internal sealed partial class EventID {
        public BatchID BatchID { get; }

        public int Sequence { get; }

        private EventID(BatchID batchID, int sequence)
            => (BatchID, Sequence) = (batchID, sequence);

        public EventID GetNext() {
            if (Sequence < MaxEventSequence) {
                return new EventID(BatchID, Sequence + 1);
            }

            throw new InvalidOperationException(
                $"Cannot generate more than {EventID.MaxEventSequence} EventIDs per Batch."
            );
        }

        public override bool Equals(object other) {
            if (other is EventID otherID)
                return BatchID.Equals(otherID.BatchID) &&
                       Sequence == otherID.Sequence;

            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(BatchID, Sequence);

        public override string ToString()
            => $"{BatchID} {Sequence:D6}";

        public static EventID GetFirst(BatchID batch) {
            ValidateTimestamp(batch.Timestamp);
            return new EventID(batch, sequence: 1);
        }

        public static EventID GetLast(BatchID batch) {
            ValidateTimestamp(batch.Timestamp);
            return new EventID(batch, sequence: MaxEventSequence);
        }
    }

    partial class EventID {
        private static readonly BitField __timestampField = new BitField(position: 32, size: 31);
        private static readonly BitField __subsecondField = new BitField(position: 25, size: 7);
        private static readonly BitField __batchSequenceField = new BitField(position: 16, size: 9);
        private static readonly BitField __eventSequenceField = new BitField(position: 0, size: 16);

        private static readonly DateTime EpochZero = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime MinTimestamp = EpochZero;
        private static readonly DateTime MaxTimestamp = EpochZero + TimeSpan.FromSeconds(__timestampField.MaxValue);

        public static readonly int MaxBatchSequence = (int)__batchSequenceField.MaxValue;
        public static readonly int MaxEventSequence = (int)__eventSequenceField.MaxValue;

        private const int SubsecondPrecision = 10; // milliseconds
        public static readonly TimeSpan TimestampPrecision = TimeSpan.FromMilliseconds(SubsecondPrecision);


        public EventID(long rawValue) {
            long secondsSinceEpoch = __timestampField.GetField(rawValue);
            long subseconds = __subsecondField.GetField(rawValue);
            long batchSequence = __batchSequenceField.GetField(rawValue);
            long eventSequence = __eventSequenceField.GetField(rawValue);

            DateTime timestamp = EpochZero
                .AddSeconds(secondsSinceEpoch)
                .AddMilliseconds(subseconds * SubsecondPrecision);

            BatchID = BatchID.DeserializeInternal(timestamp, (int)batchSequence);

            Sequence = (int)eventSequence;
        }

        public long Serialize() {
            TimeSpan t = BatchID.Timestamp - EpochZero;
            long secondsSinceEpoch = (long)t.TotalSeconds;
            long subseconds = t.Milliseconds / SubsecondPrecision;
            long sequence = BatchID.Sequence;

            return __timestampField.SetField(secondsSinceEpoch) |
                __subsecondField.SetField(subseconds) |
                __batchSequenceField.SetField(sequence) |
                __eventSequenceField.SetField(Sequence);
        }

        public static DateTime GetTimestampDate(DateTime exactDate) {
            // For details von truncating DateTimes see 
            // https://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime
            const long precisionInTicks = SubsecondPrecision * TimeSpan.TicksPerMillisecond;
            return exactDate.AddTicks(-(exactDate.Ticks % precisionInTicks));
        }

        public static void ValidateTimestamp(DateTime timestamp) {
            Check.Requires<InvalidOperationException>(
                EpochZero <= timestamp && timestamp <= MaxTimestamp,
                "The Timestamp of a BatchID must be between {0} and {1}.", EpochZero, MaxTimestamp
            );

            Check.Requires<InvalidOperationException>(
                timestamp.Kind == DateTimeKind.Utc,
                "The Timestamp of a BatchID must be of Kind Utc."
            );
        }

        private class BitField {
            public int Position { get; }
            public int Size { get; }
            public long MaxValue { get; }

            public BitField(uint position, uint size) {
                Check.Requires(position + size <= 63);

                Position = (int)position;
                Size = (int)size;
                MaxValue = Pow2(Size) - 1;
            }

            public long SetField(long fieldValue) {
                Check.Within(fieldValue, 0, MaxValue, nameof(fieldValue));
                return fieldValue << Position;
            }

            public long GetField(long value)
                => (value >> Position) & MaxValue;

            private static long Pow2(int exponent)
                => 1L << exponent;
        }
    }
}
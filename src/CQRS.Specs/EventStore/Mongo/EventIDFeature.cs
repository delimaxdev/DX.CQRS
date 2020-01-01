using DX.Cqrs.EventStore.Mongo;
using DX.Testing;
using FluentAssertions;
using System;
using Xbehave;

namespace EventStore.Mongo {
    public class EventIDFeature : Feature {
        [Scenario]
        internal void BatchIDTests(BatchID id, DateTime timestamp, bool result) {
            WHEN["creating an instance with invalid Timestamp (before MinTimestamp)", ThenIsThrown<InvalidOperationException>()] = () =>
                new BatchID(new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(-1));

            WHEN["creating an instance with invalid Timestamp (after MaxTimestamp)", ThenIsThrown<InvalidOperationException>()] = () =>
                new BatchID(new DateTime(2083, 1, 19, 3, 14, 7, DateTimeKind.Utc).AddMilliseconds(1));

            WHEN["creating an instance with a local timezoned timestamp", ThenIsThrown<InvalidOperationException>()] = () =>
                new BatchID(DateTime.Now);

            GIVEN["a new BatchID"] = () => id = new BatchID(timestamp = DateTime.UtcNow);
            THEN["its Sequence is 1"] = () => id.Sequence.Should().Be(1);
            AND["its timestamp is rounded to 10ms"] = () => (id.Timestamp.Ticks % (TimeSpan.TicksPerMillisecond * 10)).Should().Be(0);

            WHEN["calling TryToAdvance with same timestamp"] = () => result = id.TryToAdvance(timestamp);
            THEN["it should return true"] = () => result.Should().BeTrue();
            AND["its Sequence is 2"] = () => id.Sequence.Should().Be(2);

            GIVEN["a BatchID with a sequence one below maximum sequence"] = () => id = BatchID.DeserializeInternal(EventID.GetTimestampDate(timestamp), 510);
            WHEN["calling TryToAdvance"] = () => id.TryToAdvance(timestamp);
            THEN["Sequence is max sequence"] = () => id.Sequence.Should().Be(511);

            WHEN["calling TryToAdvance once more"] = () => result = id.TryToAdvance(timestamp);
            THEN["it returns false"] = () => result.Should().BeFalse();
            AND["Sequence is max"] = () => id.Sequence.Should().Be(511);

            WHEN["calling TryToAdvance with timestamp + 10 ticks"] = () => result = id.TryToAdvance(timestamp.AddTicks(10));
            THEN["it still returns false"] = () => result.Should().BeFalse();

            WHEN["calling TryToAdvance with timestamp + 10ms"] = () => result = id.TryToAdvance(timestamp = timestamp.AddMilliseconds(10));
            THEN["it returns true"] = () => result.Should().BeTrue();
            AND["its Timestamp is set to the new timestamp"] = () => id.Timestamp.Should().Be(EventID.GetTimestampDate(timestamp));
            AND["its Sequence should be 1"] = () => id.Sequence.Should().Be(1);

            WHEN["calling TryToAdvance with an invalid timestamp", ThenIsThrown<InvalidOperationException>()] = () =>
                id.TryToAdvance(new DateTime(2090, 1, 1));

            WHEN["calling TryToAdvance with a timestamp before the current timestamp", ThenIsThrown<InvalidOperationException>()] = () =>
                id.TryToAdvance(timestamp.AddMilliseconds(-10));

        }

        [Scenario]
        internal void EventIDTests(
            DateTime timestamp,
            BatchID batchID,
            EventID id,
            EventID next
        ) {
            GIVEN["a BatchID"] = () => batchID = new BatchID(timestamp = DateTime.UtcNow);
            WHEN["calling GetFirst"] = () => id = EventID.GetFirst(batchID);
            THEN["a new value with given batch ID is returned"] = () => id.BatchID.Should().Be(batchID);
            AND["its Sequence is 1"] = () => id.Sequence.Should().Be(1);

            WHEN["calling GetNext"] = () => next = id.GetNext();
            THEN["a new EventID is returned"] = () => next.Should().NotBeSameAs(id);
            AND["its BatchID is the original one"] = () => next.BatchID.Should().Be(batchID);
            AND["its Sequence is 2"] = () => next.Sequence.Should().Be(2);

            GIVEN["a EventID with max sequence"] = () => {
                id = EventID.GetFirst(batchID);

                for (int i = 2; i <= 65535; i++) {
                    id = id.GetNext();
                }

                id.Sequence.Should().Be(65535);
            };
            WHEN["calling GetNext", ThenIsThrown<InvalidOperationException>()] = () => id.GetNext();
        }


        [Scenario]
        internal void EventIDSerialization(
            BatchID batch,
            DateTime timestamp,
            long raw,
            EventID id,
            EventID deserialized
        ) {
            GIVEN["a BatchID"] = () => batch = new BatchID(timestamp = DateTime.UtcNow);
            AND["its last EventID"] = () => id = EventID.GetLast(batch);

            WHEN["calling Serialize"] = () => raw = id.Serialize();
            THEN["the last 2 bytes should contain the event sequence"] = () =>
                (raw & 0xFFFF).Should().Be(65535);
            AND["the next 9 bits should contain the batch sequence"] = () =>
                (raw >> 16 & 0x1FF).Should().Be(1);
            AND["the first 31 bits should contain the timestamp"] = () =>
                new DateTime(2015, 1, 1).AddSeconds((raw >> 32 & 0x7FFFFFFF)).Should().BeCloseTo(timestamp, precision: 1000);

            WHEN["it is serialized and deserialized"] = () => deserialized = new EventID(id.Serialize());
            THEN["its value is the same"] = () => deserialized.Should().Be(id);

            GIVEN["a timestamp with zero ms"] = () => timestamp = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            AND["the subseconds serialize to 0"] = () => Subseconds(timestamp).Should().Be(0);

            GIVEN["a timestamp with 9 ms"] = null;
            AND["the subseconds serialize to 0"] = () => Subseconds(timestamp.AddMilliseconds(9)).Should().Be(0);

            GIVEN["a timestamp with 10 ms"] = null;
            AND["the subseconds serialize to 1"] = () => Subseconds(timestamp.AddMilliseconds(10)).Should().Be(1);

            GIVEN["a timestamp with 19 ms"] = null;
            AND["the subseconds serialize to 1"] = () => Subseconds(timestamp.AddMilliseconds(19)).Should().Be(1);

            GIVEN["a timestamp with 999 ms"] = null;
            AND["the subseconds serialize to 99"] = () => Subseconds(timestamp.AddMilliseconds(999)).Should().Be(99);

            long Subseconds(DateTime ts) {
                long r = EventID.GetFirst(new BatchID(ts)).Serialize();
                return r >> 25 & 0x7F;
            };
        }
    }
}

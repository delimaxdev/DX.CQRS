using DX;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using System;
using System.Linq;
using Xbehave;

namespace EventStore.Mongo {
    public class EventIDGeneratorFeature : Feature {
        [Scenario]
        internal void BatchIDGeneratorTests(
            BatchIDGenerator gen,
            TestClock clock,
            DateTime now,
            EventIDGenerator res,
            EventID id
        ) {
            GIVEN["a generator without a last EventID"] = () => {
                clock = new TestClock();
                gen = BatchIDGenerator.Create(clock: clock);
            };

            GIVEN["a preset time"] = () => clock.SetUtcNow(now = DateTime.UtcNow);
            WHEN["calling GetBatch"] = () => res = gen.GetBatch().Result;
            AND["getting the first ID"] = () => id = res.Next();
            THEN["its event sequence is 1"] = () => id.Sequence.Should().Be(1);
            AND["its batch sequence is 1"] = () => id.BatchID.Sequence.Should().Be(1);
            AND["its timestamp is correct"] = () => id.BatchID.Timestamp.Should().BeCloseTo(now, precision: 10);

            GIVEN["a new time very close to last"] = () => clock.SetUtcNow(now.AddTicks(1));
            WHEN["calling GetBatch again"] = () => id = gen.GetBatch().Result.Next();
            THEN["the event sequence of first EventID is 1"] = () => id.Sequence.Should().Be(1);
            AND["its batch sequence is 2"] = () => id.BatchID.Sequence.Should().Be(2);

            GIVEN["a new time"] = () => clock.SetUtcNow(now = now.AddMilliseconds(10));
            WHEN["calling GetBatch"] = () => id = gen.GetBatch().Result.Next();
            THEN["the event sequence of first EventID is 1"] = () => id.Sequence.Should().Be(1);
            AND["its batch sequence is 1"] = () => id.BatchID.Sequence.Should().Be(1);
            AND["its timestamp is correct"] = () => id.BatchID.Timestamp.Should().BeCloseTo(now, precision: 10);

            GIVEN["a new time"] = () => clock.SetUtcNow(now = now.AddMilliseconds(10));
            THEN["calling GetBatch for 511 times takes less than 10ms"] = () => {
                Action action = () => Enumerable.Range(1, 511).ForEach(i => res = gen.GetBatch().Result);
                action.ExecutionTime().Should().BeLessThan(100.Milliseconds());
            };
            AND["the batch sequence of the first EventID is 511 (max)"] = () => (id = res.Next()).BatchID.Sequence.Should().Be(511);
            Then["calling GetBatch once more", ThrowsA<EventStoreException>()] = () => gen.GetBatch();

            GIVEN["a clock the advances with each call"] = () => clock.SetUtcNow(new PresetTimes(now, now.AddTicks(1), now = now.AddMilliseconds(20)));
            WHEN["calling GetBatch"] = () => id = gen.GetBatch().Result.Next();
            AND["its batch sequence is 1"] = () => id.BatchID.Sequence.Should().Be(1);
            AND["its timestamp is correct"] = () => id.BatchID.Timestamp.Should().BeCloseTo(now, precision: 10);
        }

        [Scenario]
        internal void EventIDGeneratorTests(EventIDGenerator gen, BatchID batch, EventID res) {
            GIVEN["a batch ID"] = () => batch = new BatchID(DateTime.UtcNow);
            AND["a generator"] = () => gen = new EventIDGenerator(batch);

            WHEN["calling Next"] = () => res = gen.Next();
            THEN["the sequence of the EventID is 1"] = () => res.Sequence.Should().Be(1);
            AND["its batch sequence is 1"] = () => res.BatchID.Sequence.Should().Be(1);

            WHEN["calling Next again"] = () => res = gen.Next();
            THEN["the sequence of the EventID is 2"] = () => res.Sequence.Should().Be(2);

            GIVEN["a new generator"] = () => gen = new EventIDGenerator(batch);
            WHEN["calling Next 65535 times"] = () => Enumerable.Range(1, 65535).ForEach(i => res = gen.Next());
            THEN["the sequence of the EventID is 6555 (max)"] = () => res.Sequence.Should().Be(65535);

            WHEN["calling Next once more", ThenIsThrown<InvalidOperationException>()] = () => gen.Next();
        }
    }
}



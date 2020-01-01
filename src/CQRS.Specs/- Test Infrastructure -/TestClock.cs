using DX.Cqrs.Commons;
using System;
using System.Collections.Generic;

namespace DX.Testing
{
    public class TestClock : Clock {
        private Func<DateTime> _utcNow = () => DateTime.UtcNow;
        private Func<DateTime> _now = () => DateTime.Now;

        public override DateTime Now => _now();

        public override DateTime UtcNow => _utcNow();

        public TestClock SetUtcNow(Func<DateTime> now) {
            _utcNow = now;
            return this;
        }

        public TestClock SetUtcNow(DateTime now)
            => SetUtcNow(() => now);

        public TestClock SetUtcNow(PresetTimes times)
            => SetUtcNow(times.Provide);


        public TestClock SetNow(Func<DateTime> now) {
            _now = now;
            return this;
        }

        public TestClock SetNow(DateTime now)
            => SetNow(() => now);

        public TestClock SetNow(PresetTimes times)
            => SetNow(times.Provide);
    }

    public class PresetTimes {
        private readonly Queue<Func<DateTime>> _results = new Queue<Func<DateTime>>();

        public PresetTimes(params DateTime[] presetResults) {
            presetResults.ForEach(t => EnqueueResult(t));
        }

        public PresetTimes EnqueueResult(Func<DateTime> time) {
            _results.Enqueue(time);
            return this;
        }

        public PresetTimes EnqueueResult(DateTime now)
            => EnqueueResult(() => now);

        public DateTime Provide()
            => _results.Dequeue()();
    }
}

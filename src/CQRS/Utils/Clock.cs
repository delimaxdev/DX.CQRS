using System;

namespace DX.Cqrs.Commons
{
    public abstract class Clock {
        public static readonly Clock System = new SystemClock();

        public abstract DateTime Now { get; }

        public abstract DateTime UtcNow { get; }

        private class SystemClock : Clock {
            public override DateTime Now => DateTime.Now;

            public override DateTime UtcNow => DateTime.UtcNow;
        }
    }
}

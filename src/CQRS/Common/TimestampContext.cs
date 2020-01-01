using System;

namespace DX.Cqrs.Common
{
    public class TimestampContext {
        public DateTime Timestamp { get; }

        public TimestampContext(DateTime timestamp) {
            Timestamp = timestamp;
        }
    }
}
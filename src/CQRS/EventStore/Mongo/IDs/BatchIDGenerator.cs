using DX.Cqrs.Commons;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DX.Cqrs.EventStore.Mongo
{
    internal abstract partial class BatchIDGenerator {
        public abstract Task<EventIDGenerator> GetBatch();

        public static BatchIDGenerator Create(EventID? lastID = null, Clock? clock = null) {
            clock = clock ?? Clock.System;

            return lastID != null ?
                new DefaultEventIDGenerator(lastID, clock) :
                new DefaultEventIDGenerator(clock);
        }
    }

    partial class BatchIDGenerator {
        class DefaultEventIDGenerator : BatchIDGenerator {
            private readonly Clock _clock;
            private bool _warningLogged = false;
            private BatchID _currentBatchID;

            public DefaultEventIDGenerator(Clock clock) {
                _clock = Check.NotNull(clock, nameof(clock));
                _currentBatchID = BatchID.Empty;
            }

            public DefaultEventIDGenerator(EventID lastID, Clock clock) {
                _clock = Check.NotNull(clock, nameof(clock));
                _currentBatchID = Check.NotNull(lastID, nameof(lastID)).BatchID;
            }

            public override async Task<EventIDGenerator> GetBatch() {
                const int MaxWaits = 10;

                for (int waits = 0; waits < MaxWaits; waits++) {
                    if (TryToGetNextBatchID()) {
                        return new EventIDGenerator(_currentBatchID);
                    }

                    LogWaitWarning();
                    await Task.Delay(EventID.TimestampPrecision);
                }

                throw new EventStoreException(
                    "Failed to generate a new EventID. Something seems to be wrong with the given Clock: the " +
                    "returned time didn't change for a much longer period than expected."
                );
            }

            private bool TryToGetNextBatchID() {
                DateTime now = _clock.UtcNow;

                if (now < _currentBatchID.Timestamp) {
                    throw new EventStoreException(
                        $"The clock given to the {nameof(BatchIDGenerator)} returned a time earlier than a " +
                        $"previously known time."
                    );
                }

                return _currentBatchID.TryToAdvance(now);
            }

            private void LogWaitWarning() {
                if (!_warningLogged) {
                    _warningLogged = true;

                    Trace.TraceWarning(
                        $"The {nameof(BatchIDGenerator)} generated more than {EventID.MaxBatchSequence} batches within the " +
                        $"last {EventID.TimestampPrecision.TotalMilliseconds} ms and needed to wait to generate new IDs."
                    );
                }
            }
        }
    }
}

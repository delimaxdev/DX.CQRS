using DX.Cqrs;
using DX.Cqrs.Common;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DX.Testing
{
    public class TestEventStore : IEventStore {
        private readonly List<StorageItem> _store = new List<StorageItem>();

        public IEnumerable<EventBatch> All => _store.Select(x => x.Batch);

        public IEventCriteria CreateCriteria(Action<IEventCriteriaBuilder> builder) {
            return new CriteriaFake();
        }
        
        public IEventStoreTransaction UseTransaction(ITransaction transaction) {
            return new TestEventStoreTransaction(_store);
        }

        private class CriteriaFake : IEventCriteria {

        }

        private class StorageItem {
            public Type StreamType { get; }

            public EventBatch Batch { get;  }

            public StorageItem(Type streamType, EventBatch batch)
                => (StreamType, Batch) = (streamType, batch);
        }

        private class TestEventStoreTransaction : IEventStoreTransaction {
            private readonly List<StorageItem> _store;

            private IEnumerable<EventBatch> All => _store.Select(x => x.Batch);

            public TestEventStoreTransaction(List<StorageItem> store)
                => _store = store;


            public Task Save<T>(EventBatch<T> batch) {
                Check.NotNull(batch, nameof(batch));

                lock (_store) {
                    _store.Add(new StorageItem(typeof(T), batch));
                }

                return Task.CompletedTask;
            }

            public Task<bool> Exists<T>(StreamLocator<T> s) {
                EventBatch[] batches;
                lock (_store) {
                    batches = GetCore(s).ToArray();
                }
                return Task.FromResult(batches.Any());
            }

            public Task<IAsyncEnumerable<RecordedEvent>> Get<T>(StreamLocator<T> s) {
                Check.NotNull(s, nameof(s));
                
                RecordedEvent[] events;
                lock (_store) {
                    events = GetCore(s)
                        .SelectMany(x => x.Events)
                        .ToArray();
                }

                return Task.FromResult(events.ToAsyncEnumerable());
            }

            public Task<IAsyncEnumerable<RecordedEvent>> Get(IEventCriteria criteria) {
                RecordedEvent[] events;
                lock (_store) {
                    events = All.SelectMany(x => x.Events).ToArray();
                }

                // HACK: We do not consider criteria yet!
                return Task.FromResult(events.ToAsyncEnumerable());
            }

            public Task<IAsyncEnumerable<EventBatch>> GetAll(IEventCriteria spec) {
                EventBatch[] events;
                lock (_store) {
                    events = All.ToArray();
                }

                return Task.FromResult(events.ToAsyncEnumerable());
            }

            private IEnumerable<EventBatch> GetCore<T>(StreamLocator<T> s) => _store
                .Where(x => x.StreamType == typeof(T) && x.Batch.StreamID.Equals(s.StreamID))
                .Select(x => x.Batch);
        }
    }
}

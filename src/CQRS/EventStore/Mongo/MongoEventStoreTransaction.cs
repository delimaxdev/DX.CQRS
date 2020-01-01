using DX.Cqrs.Mongo.Facade;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DX.Cqrs.EventStore.Mongo {
    public class MongoEventStoreTransaction : IEventStoreTransaction {
        private readonly MongoEventStore _store;
        private readonly IMongoFacadeTransaction _transaction;

        public MongoEventStoreTransaction(MongoEventStore store, IMongoFacadeTransaction transaction) {
            _store = store;
            _transaction = transaction;
        }

        public async Task Save<T>(EventBatch<T> s) {
            Check.NotNull(s, nameof(T));
            Check.Requires(
                s.Events.Count <= EventID.MaxEventSequence,
                nameof(s), "A single batch can contain at most {0} events.", EventID.MaxEventSequence
            );

            StreamConfiguration config = _store.GetStreamConfig<T>();

            // Now checked by the serializer itself...
            //RecordedEvent eventOfUnregisteredType = s.Events
            //    .FirstOrDefault(x => !_serializationConfiguration.EventClasses.Contains(x.Event.GetType()));

            //if (eventOfUnregisteredType != null) {
            //    throw new EventStoreConfigurationException(
            //        $"Event type {eventOfUnregisteredType.Event.GetType().Name} was not configured. Call " +
            //        $"RegisterEventClass when calling MongoEventStore.Configure.");
            //}

            if (s.Events.Any()) {
                EventIDGenerator idGenerator = await _store.GetBatch();

                foreach (RecordedEvent e in s.Events) {
                    e.ID = idGenerator.Next();
                }

                await _transaction
                    .GetCollection<RecordedEvent>(MongoEventStore.CollectionName)
                    .InsertManyAsync(s.Events);

                StreamInfo info = new StreamInfo(s.StreamID);

                await _transaction
                    .GetCollection<StreamInfo>(MongoEventStore.GetStreamInfoName(config))
                    .UpsertAsync(x => x.StreamID, info.StreamID, info);
            }
        }

        public Task<bool> Exists<T>(StreamLocator<T> s) {
            StreamConfiguration config = _store.GetStreamConfig<T>();
            return _transaction.GetCollection<StreamInfo>(MongoEventStore.GetStreamInfoName(config)).Exists(i => i.StreamID, s.StreamID);
        }

        public Task<IAsyncEnumerable<RecordedEvent>> Get<T>(StreamLocator<T> s) {
            return _transaction.GetCollection<RecordedEvent>(MongoEventStore.CollectionName).FindAll(x => x.StreamID, s.StreamID);
        }

        public Task<IAsyncEnumerable<RecordedEvent>> Get(IEventCriteria criteria) {
            EventCriteria c = (EventCriteria)criteria;
            return GetAll(c.Filter);
        }


        public async Task<IAsyncEnumerable<EventBatch>> GetAll(IEventCriteria? criteria = null) {
            IAsyncEnumerable<RecordedEvent> events = await Get(criteria ?? EventCriteria.Empty);
            return new BatchEnumerable(events);
        }


        private Task<IAsyncEnumerable<RecordedEvent>> GetAll(FilterDefinition<RecordedEvent> filter) {
            return _transaction.GetCollection<RecordedEvent>(MongoEventStore.CollectionName).FindAll(
                filter,
                Builders<RecordedEvent>.Sort.Ascending(x => x.ID));
        }

        private class BatchEnumerable : IAsyncEnumerable<EventBatch> {
            private readonly IAsyncEnumerable<RecordedEvent> _events;

            public BatchEnumerable(IAsyncEnumerable<RecordedEvent> events)
                => _events = events;

            public IAsyncEnumerator<EventBatch> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new BatchEnumerator(_events.GetAsyncEnumerator(cancellationToken));
        }

        private class BatchEnumerator : IAsyncEnumerator<EventBatch> {
            private readonly IAsyncEnumerator<RecordedEvent> _events;
            private bool _endIsReached = false;
            private bool _isInitialInvoke = true;

            public EventBatch Current { get; private set; } = null!;

            public BatchEnumerator(IAsyncEnumerator<RecordedEvent> events)
                => _events = events;

            public ValueTask DisposeAsync()
                => _events.DisposeAsync();

            public async ValueTask<bool> MoveNextAsync() {
                // When MoveNextAsync is called the first time, we position _events at the first event of 
                // the first batch
                if (_isInitialInvoke) {
                    _isInitialInvoke = false;
                    _endIsReached = !await _events.MoveNextAsync();
                }

                if (_endIsReached == false) {
                    // Here _events is guaranteed to be positioned at the first event of the next (or first) 
                    // batch:
                    //   (1) if the source enumerable was empty, the first "if" would have set _endIsReached 
                    //       to true or
                    //   (2) if the last call to ReadNextBatch has read all available events, it would have
                    //       set _endIsReached to true. Otherwise it positions _events at the first event
                    //       of the next batch.
                    await ReadNextBatch();
                    return true;
                }

                return false;
            }

            private async Task ReadNextBatch() {
                RecordedEvent e = _events.Current;

                BatchID batchID = GetBatchID(e);
                object streamID = e.StreamID;
                List<RecordedEvent> buffer = new List<RecordedEvent>() { e };

                bool nextBatchFound = false;
                while (await _events.MoveNextAsync()) {
                    e = _events.Current;

                    if (GetBatchID(e) != batchID) {
                        nextBatchFound = true;
                        break;
                    }

                    buffer.Add(e);
                }

                _endIsReached = !nextBatchFound;
                Current = new EventBatch(streamID, buffer);
            }

            private static BatchID GetBatchID(RecordedEvent e) {
                EventID eventID = Expect.IsOfType<EventID>(e.ID);
                return eventID.BatchID;
            }
        }
    }
}
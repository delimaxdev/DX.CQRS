using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Mongo;
using DX.Cqrs.Mongo.Facade;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DX.Cqrs.EventStore.Mongo
{
    public class MongoEventStore : IEventStore {
        internal const string CollectionName = "Events";
        private static ITypeNameResolver __typeNameResolver = new UnsetTypeNameResolver();

        private readonly IMongoFacade _db;
        private readonly StreamCollection _streams;
        private BatchIDGenerator? _idGenerator;

        public MongoEventStore(IMongoFacade db, EventStoreSettings settings) {
            _db = Check.NotNull(db, nameof(db));
            _streams = new StreamCollection(settings.Streams);
        }

        internal MongoEventStore(IMongoFacade db, EventStoreSettings settings, BatchIDGenerator idGenerator) : this(db, settings) {
            _idGenerator = idGenerator;
        }

        public static void ConfigureSerialization(MongoEventStoreSerializatonSettings settings) {
            // TODO: Is there a less global way??
            BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;

            __typeNameResolver = settings.TypeNameResolver;
            IBsonSerializer streamIDSerializer = new CastSerializer<object>(settings.StreamIDSerializer);

            BsonClassMap.RegisterClassMap<RecordedEvent>(m => {
                m.MapIdProperty(x => x.ID).SetSerializer(EventIDSerializer.CastInstance);
                m.MapProperty(x => x.StreamID).SetSerializer(streamIDSerializer);
                m.MapProperty(x => x.Event).SetElementName("e").SetSerializer(settings.EventSerializer);
                m.MapProperty(x => x.Metadata).SetElementName("m").SetSerializer(settings.MetadataSerializer);
                m.MapCreator(x => new RecordedEvent(x.StreamID, x.Event, x.Metadata));
            });

            BsonClassMap.RegisterClassMap<StreamInfo>(m => {
                m.MapIdField(x => x.StreamID).SetSerializer(streamIDSerializer);
                m.MapCreator(x => new StreamInfo(x.StreamID));
            });

            // Required for the 'Max' query in 'EnsureGenerator'
            BsonSerializer.RegisterSerializer(EventIDSerializer.Instance);
        }

        public async Task Upgrade() {
            IReadOnlyCollection<string> existingCollections = await _db.GetCollectionNamesAsync();

            if (!existingCollections.Contains(CollectionName)) {
                await _db.CreateCollectionAsync(CollectionName);
                await _db.GetCollection<RecordedEvent>(CollectionName).CreateIndex("StreamID");
            }

            string[] missingCollections = _streams
                .Select(s => GetStreamInfoName(s))
                .Except(existingCollections)
                .ToArray();

            await Task.WhenAll(missingCollections.Select(x => _db.CreateCollectionAsync(x)));
        }

        public IEventStoreTransaction UseTransaction(ITransaction transaction) {
            IMongoFacadeTransaction facadeTx = _db.UseTransaction(transaction);
            return new MongoEventStoreTransaction(this, facadeTx);
        }

        internal async Task<EventIDGenerator> GetBatch() {
            return await (await EnsureGenerator()).GetBatch();
        }

        internal StreamConfiguration GetStreamConfig<T>() {
            bool streamTypeRegistered = _streams.TryGetValue(typeof(T), out StreamConfiguration config);

            Check.Requires<EventStoreConfigurationException>(
                streamTypeRegistered,
                "Stream type {0} was not configured. Call RegisterStreamType " +
                "when calling MongoEventStore.Configure.", typeof(T).Name
            );

            return config;
        }

        public IEventCriteria CreateCriteria(Action<IEventCriteriaBuilder> builder) {
            var b = new EventCriteriaBuilder();
            builder(b);
            return b.BuildCriteria(__typeNameResolver);
        }

        internal static string GetStreamInfoName(StreamConfiguration c)
            => $"{c.Name}_Info";

        private async Task<BatchIDGenerator> EnsureGenerator() {
            if (_idGenerator == null) {
                using (ITransaction tx = await _db.StartTransactionAsync()) {
                    Maybe<EventID> lastID = await _db
                        .UseTransaction(tx)
                        .GetCollection<RecordedEvent>(CollectionName)
                        .Max<EventID>("_id");

                    _idGenerator = BatchIDGenerator.Create(lastID.OrDefault());
                }
            }

            return _idGenerator;
        }

        private class StreamCollection : KeyedCollection<Type, StreamConfiguration> {
            public StreamCollection(IEnumerable<StreamConfiguration> streams)
                => streams.ForEach(Add);

            protected override Type GetKeyForItem(StreamConfiguration item)
                => item.Type;
        }

        private class UnsetTypeNameResolver : ITypeNameResolver {
            public string GetTypeName(Type type) {
                throw new InvalidOperationException(
                    "No ITypeNameResolver has been set. Make sure you have called " +
                    "MongoEventStore.ConfigureSerialization before first use.");
            }
        }
    }

    public class EventStoreSettings {
        public IReadOnlyCollection<StreamConfiguration> Streams { get; }

        public EventStoreSettings(IReadOnlyCollection<StreamConfiguration> streams) {
            Streams = Check.NotNull(streams, nameof(streams));
        }
    }

    public class MongoEventStoreSerializatonSettings {
        public IBsonSerializer StreamIDSerializer { get; }

        public IBsonSerializer<object> EventSerializer { get; }

        public IBsonSerializer<object> MetadataSerializer { get; }

        public ITypeNameResolver TypeNameResolver { get; }

        public MongoEventStoreSerializatonSettings(
            IBsonSerializer streamIDSerializer,
            IBsonSerializer<object> eventSerializer,
            IBsonSerializer<object> metadataSerializer,
            ITypeNameResolver typeNameResolver
        ) {
            StreamIDSerializer = Check.NotNull(streamIDSerializer, nameof(streamIDSerializer));
            EventSerializer = Check.NotNull(eventSerializer, nameof(eventSerializer));
            MetadataSerializer = Check.NotNull(metadataSerializer, nameof(metadataSerializer));
            TypeNameResolver = Check.NotNull(typeNameResolver, nameof(typeNameResolver));
        }
    }
}
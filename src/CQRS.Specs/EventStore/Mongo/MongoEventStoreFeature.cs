using DX;
using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Testing;
using FluentAssertions;
using global::Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbehave;

namespace EventStore.Mongo {
    public class MongoEventStoreFeature : BsonSerializationFeature {
        [Scenario]
        internal void Save(
            MongoFake db,
            MongoEventStore store,
            IEventStoreTransaction tx,
            PresetIDGenerator generator,
            EventBatch<Customer> s,
            BatchID batch
        ) {
            GIVEN["a configured store"] = () => store = CreateStore(
                new List<StreamConfiguration> {
                    new StreamConfiguration(typeof(Customer), "customer"),
                    new StreamConfiguration(typeof(OrderProcessor), "order_processor")
                },
                db = new MongoFake(),
                generator = new PresetIDGenerator()
            );

            Given["a transaction"] = async () => tx = store.UseTransaction(await db.StartTransactionAsync());

            Then["saving an unregistered stream type", ThrowsA<EventStoreConfigurationException>()] = () =>
                tx.Save(CreateStream<Order>(streamID: Guid.NewGuid()));

            GIVEN["a stream with some events"] = () => s = CreateStream<Customer>(
                streamID: Guid.NewGuid(),
                new Customer.Created(),
                new Customer.Relocated { OldAddress = "ADR 1", NewAddress = "ADR 2" }
            );

            When["saving the stream"] = () => {
                batch = new BatchID(DateTime.UtcNow);
                generator.Enqueue(batch);
                return tx.Save(s);
            };

            THEN["an EventID is assigned to each event"] = () => {
                EventIDGenerator gen = new EventIDGenerator(batch);

                s.Events.Select(x => x.ID).Should()
                    .AllBeOfType<EventID>().And
                    .ContainInOrder(gen.Next(), gen.Next());
            };

            AND["the events are persisted properly"] = () =>
                db.Log.Should().BeExactly(b => b
                    .Transaction(t => t
                        .InsertMany("Events", s.Events.ToArray()) // TODO: Better interface on Fake...
                        .Upsert("customer_Info", s.StreamID, new StreamInfo(s.StreamID))
                    )
                );

            List<RecordedEvent> act = default;

            WHEN["getting the saved stream"] = () =>
                act = tx.Get(new StreamLocator<Customer>(s.StreamID)).Result.ToList().Result;

            THEN["it contains the original events"] = () =>
                act.Should().BeEquivalentTo(s.Events);
        }

        [Scenario]
        public void GetAll(MongoEventStore store, IEventStoreTransaction tx, List<EventBatch> exp, List<EventBatch> act) {
            MongoFake db = default;

            GIVEN["a configured store"] = () => store = CreateStore(
                new List<StreamConfiguration> {
                    new StreamConfiguration(typeof(Customer), "customer"),
                    new StreamConfiguration(typeof(Order), "order")
                },
                db = new MongoFake());

            Given["a transaction"] = async () => tx = store.UseTransaction(await db.StartTransactionAsync());


            WHEN["storing some streams"] = () => {
                exp = new List<EventBatch>();
                Guid customerID = Guid.NewGuid();

                Save(Order.CreateOrderWithProducts());
                Save(Customer.CreateCustomer(customerID));
                Save(Order.CreateOrderWithProducts());
                Save(CreateStream<Customer>(customerID, new Customer.Promoted()));

                void Save<T>(EventBatch<T> batch) {
                    tx.Save(batch).Wait();
                    exp.Add(batch);
                };
            };

            AND["calling GetAll"] = () => {
                db.BatchSize = 2;
                act = tx.GetAll().Result.ToList().Result;
            };

            THEN["all stored events are returned in original order"] = () =>
                act.Should().BeEquivalentTo(exp, o => o.WithStrictOrdering());
        }

        [Scenario]
        public void Upgrade(MongoEventStore store) {
            MongoFake db = default;

            GIVEN["an empty DB and one configured stream"] = () => store = CreateStore(
                new List<StreamConfiguration> {
                    new StreamConfiguration(typeof(Order), "order")
                },
                db = new MongoFake(),
                new PresetIDGenerator(),
                callUpgrade: false
            );

            When["calling Upgrade"] = () => store.Upgrade();

            THEN["the Events collection is created with index on StreamID"] = () => db.Log.Should().Contain(b => b
                .CreateCollection("Events")
                .CreateIndex("Events", "StreamID"));


            GIVEN["a proper configuration"] = () => {
                store = CreateStore(
                    new List<StreamConfiguration> {
                        new StreamConfiguration(typeof(Customer), "customer"),
                        new StreamConfiguration(typeof(Order), "order"),
                        new StreamConfiguration(typeof(OrderProcessor), "order_processor")
                    },
                    db,
                    new PresetIDGenerator(),
                    callUpgrade: false,
                    preserveLog: true
                );
            };

            When["calling Upgrade"] = () => store.Upgrade();

            THEN["the missing collections are created"] = () => {
                db.Log.Should().Contain(b => b
                    .CreateCollection("customer_Info")
                    .CreateCollection("order_processor_Info"));
            };
        }

        private static MongoEventStore CreateStore(
            List<StreamConfiguration> streams,
            MongoFake db = null,
            BatchIDGenerator idGenerator = null,
            bool preserveLog = false,
            bool callUpgrade = true
        ) {
            db = db ?? new MongoFake();
            idGenerator = idGenerator ?? new LoggingIDGenerator();

            ConfigureSerialization(new GuidSerializer(BsonType.Binary));

            MongoEventStore store = new MongoEventStore(
                db,
                new EventStoreSettings(streams),
                idGenerator
            );

            if (callUpgrade) {
                store.Upgrade().Wait();
            }

            if (!preserveLog) {
                db.Log.Clear();
            }

            return store;
        }

        private static EventBatch<T> CreateStream<T>(Guid streamID, params EventBase[] events) {
            DefaultEventMetadata metadata = new DefaultEventMetadata(DateTime.Today);
            return new EventBatch<T>(
                streamID, 
                events.Select(e => 
                    new RecordedEvent(streamID, e, metadata)
            ).ToArray());
        }

        private static Guid EnsureNotEmpty(Guid value) {
            return value == Guid.Empty ?
               Guid.NewGuid() :
               value;
        }
        internal class PresetIDGenerator : BatchIDGenerator {
            private readonly Queue<BatchID> _batches;

            public PresetIDGenerator(params BatchID[] batchesToReturn) {
                _batches = new Queue<BatchID>(batchesToReturn);
            }

            public void Enqueue(params BatchID[] batchIDs)
                => batchIDs.ForEach(_batches.Enqueue);

            public override Task<EventIDGenerator> GetBatch() {
                return Task.FromResult(new EventIDGenerator(_batches.Dequeue()));
            }
        }

        internal class LoggingIDGenerator : BatchIDGenerator {
            BatchID _current = new BatchID(DateTime.UtcNow);

            public List<BatchID> ReturnedBatchIDs { get; } = new List<BatchID>();

            public override Task<EventIDGenerator> GetBatch() {
                ReturnedBatchIDs.Add(_current);
                EventIDGenerator result = new EventIDGenerator(_current);
                _current.TryToAdvance(DateTime.UtcNow).Should().BeTrue();
                return Task.FromResult(result);
            }
        }

        [ContractContainer]
        public class Customer {
            public static EventBatch<Customer> CreateCustomer(Guid customerID = default) {
                return CreateStream<Customer>(
                    EnsureNotEmpty(customerID),
                    new Created(),
                    new Relocated { OldAddress = "ADR1", NewAddress = "ADR2" }
                );
            }

            [Contract]
            public class Created : EventBase { }

            [Contract]
            public class Relocated : EventBase {
                public string OldAddress { get; set; }
                public string NewAddress { get; set; }
            }

            [Contract]
            public class Promoted : EventBase { }
        }

        [ContractContainer]
        class Order {
            public static EventBatch<Order> CreateOrderWithProducts(Guid orderID = default) {
                return CreateStream<Order>(
                    EnsureNotEmpty(orderID),
                    new Created(),
                    new ProductAdded { ProductID = "PROD1" },
                    new ProductAdded { ProductID = "PROD2" }
                );
            }

            [Contract]
            public class Created : EventBase { }

            [Contract]
            public class ProductAdded : EventBase {
                public string ProductID { get; set; }
            }
        }

        class OrderProcessor {
        }
    }
}
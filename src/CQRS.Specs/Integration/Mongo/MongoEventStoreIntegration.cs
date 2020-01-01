using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs;
using DX.Cqrs.Common;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Testing;
using EventStore.Mongo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbehave;
using Xunit.Abstractions;

namespace Integration.Mongo {
    public class MongoEventStoreIntegration : MongoFeature {
        private const string ModuleCode = "TEST"; // Defined with ContractAssemblyAttribute

        public MongoEventStoreIntegration(ITestOutputHelper output) : base(output) { }

        [Scenario]
        internal void SaveAndGet(
            MongoEventStore s,
            ITransaction tx,
            IEventStoreTransaction storeTx,
            List<RecordedEvent> expEvents,
            List<BsonDocument> actEvents,
            List<RecordedEvent> filteredEvents,
            Customer cust,
            Order firstOrder,
            Order secondOrder
        ) {
            Given["a configured event store"] = async () => {
                ConfigureSerialization(new GuidSerializer());
                s = await CreateStore();
            };

            USING["a transaction"] = () => {
                tx = DB.StartTransactionAsync().Result;
                storeTx = s.UseTransaction(tx);
                return tx;
            };

            When["saving a new customer"] = () => Save(cust = Customer.CreateExampleCustomer());
            THEN["all events are properly serialized and stored"] = () => AssertCorrectlySaved(cust);

            When["saving an order"] = () => Save(firstOrder = Order.CreateExampleOrder());
            And["saving a second order"] = () => Save(secondOrder = Order.CreateExampleOrder());
            THEN["the order is properly stored"] = () => AssertCorrectlySaved(secondOrder);

            WHEN["changing the customer"] = () => {
                cust.ClearChanges();
                cust.AddEvent(new Customer.Promoted());
            };

            And["saving it"] = () => Save(cust);
            THEN["it is stored"] = () => AssertCorrectlySaved(cust);

            Given["a new store instance"] = async () => s = await CreateStore();

            USING["a transaction"] = () => {
                tx = DB.StartTransactionAsync().Result;
                storeTx = s.UseTransaction(tx);
                return tx;
            };

            Given["a third order"] = () => Save(Order.CreateExampleOrder());

            WHEN["getting all events"] = () => actEvents = GetEvents();

            THEN["they are returned in chronological order"] = () =>
                actEvents.Should().BeEquivalentTo(expEvents.Select(r => GetExpectedBson(r)), o => o.WithStrictOrdering());

            When["getting only certain events"] = async () => filteredEvents = await (await storeTx.Get(
                s.CreateCriteria(c => {
                    c.Stream(cust.ID);
                    c.Stream(firstOrder.ID);
                    c.Type<Customer.Created>();
                    c.Type<Order.Created>();
                }))).ToList();

            THEN["Get only returns matching events in order"] = () => {
                RecordedEvent[] exp = expEvents.Where(e =>
                    (e.Event is Customer.Created || e.Event is Order.Created) &&
                    (e.StreamID.Equals(cust.ID) || e.StreamID.Equals(firstOrder.ID)))
                .ToArray();

                filteredEvents.Should().BeEquivalentTo(exp, o => o.WithStrictOrdering());
            };

            async Task Save<T>(Aggregate<T> x) where T : class {
                if (expEvents == null)
                    expEvents = new List<RecordedEvent>();

                expEvents.AddRange(x.Changes);
                await storeTx.Save(x.GetChanges());
                await tx.CommitAsync();
            };

            void AssertCorrectlySaved<T>(Aggregate<T> x) where T : class {
                GetEvents(x.ID).Should().BeEquivalentTo(x.GetExpectedBson());
            };
        }

        [Scenario]
        public void AssumeThatBsonEquaivalenceDoesNotConsiderTheOrderOfTheElements() {
            CUSTOM["assumption"] = () => {
                DateTime registrationDate = DateTime.Now;
                Guid streamID = Guid.NewGuid();

                var act = new BsonDocument {
                    { "_id", 16235262662L},
                    { "_t", "customer_creatd" },
                    { "name", "Test customer" },
                    { "revenue", 1000.25353M },
                    { "registration_date", registrationDate },
                    { "StreamID", new BsonBinaryData(streamID) }
                };

                act = new BsonDocument {
                    { "e", act }
                };

                var exp = new BsonDocument {
                    { "_t", "customer_creatd" },
                    { "name", "Test customer" },
                    { "_id", 16235262662L},
                    { "registration_date", registrationDate },
                    { "revenue", 1000.25353M },
                    { "StreamID", new BsonBinaryData(streamID) }
                };

                exp = new BsonDocument {
                    { "e", exp }
                };

                act.Should().BeEqivalentTo(exp);

                List<BsonDocument> expList = new List<BsonDocument> { exp };
                List<BsonDocument> actList = new List<BsonDocument> { act };

                actList.Should().BeEquivalentTo(expList);
            };
        }

        private async Task<MongoEventStore> CreateStore() {
            var streams = new List<StreamConfiguration> {
                    new StreamConfiguration(typeof(Customer), Customer.BsonName),
                    new StreamConfiguration(typeof(Order), Order.BsonName) };

            MongoEventStore s = new MongoEventStore(DB, new EventStoreSettings(streams));

            await s.Upgrade();
            return s;
        }

        private List<BsonDocument> GetEvents(Guid? aggregateID = null) {
            FilterDefinition<BsonDocument> filter = aggregateID != null ?
                Builders<BsonDocument>.Filter.Eq("StreamID", aggregateID) :
                Builders<BsonDocument>.Filter.Empty;

            var events = Env.DB.GetCollection<BsonDocument>("Events")
                .Find(filter)
                .ToList();

            return events;
        }

        private static BsonDocument GetExpectedBson(RecordedEvent r) {
            BsonDocument eventBson = ((EventBase)r.Event).GetExpectedBson();

            EventID id = (EventID)r.ID;
            DefaultEventMetadata metadata = (DefaultEventMetadata)r.Metadata;

            BsonDocument bson = new BsonDocument {
                { "_id", id.Serialize() },
                { "StreamID", BsonValue.Create(r.StreamID) },
                { "m", new BsonDocument {
                    { "ts", metadata.Timestamp },
                    { "c", BsonNull.Value }
                } },
                { "e", eventBson }
            };

            return bson;
        }

        public class Aggregate<T> where T : class {
            public Guid ID { get; set; } = Guid.NewGuid();
            public List<RecordedEvent> Changes { get; } = new List<RecordedEvent>();

            public List<RecordedEvent> Events { get; } = new List<RecordedEvent>();

            public T AddEvent(EventBase e) {
                var r = new RecordedEvent(ID, e, new DefaultEventMetadata(DateTime.Today));
                Events.Add(r);
                Changes.Add(r);
                return (T)(object)this;
            }

            public EventBatch<T> GetChanges()
                => new EventBatch<T>(ID, Changes);

            public void ClearChanges()
                => Changes.Clear();

            public BsonDocument[] GetExpectedBson() {
                return Events.Select(r => MongoEventStoreIntegration.GetExpectedBson(r)).ToArray();
            }
        }

        [ContractContainer(Customer.BsonName)]
        public class Customer : Aggregate<Customer> {
            public const string BsonName = "customer";

            public static Customer CreateExampleCustomer() {
                return new Customer()
                    .AddEvent(new Created { Name = "Test customer", RegistrationDate = DateTime.Now.Date, Revenue = 1000.25353M })
                    .AddEvent(new Relocated { OldAddress = "Address 1", NewAddress = "Address 2" });
            }

            [Contract(EventName)]
            public class Created : EventBase {
                public const string NameName = "name";
                private const string RegistrationDateName = "registration_date";
                private const string RevenueName = "revenue";
                public const string EventName = "customer_created";

                public Created() : base(ModuleCode, Customer.BsonName, EventName) { }

                [ContractMember(NameName)]
                public string Name { get; set; }

                [ContractMember(RegistrationDateName)]
                public DateTime RegistrationDate { get; set; }

                [ContractMember(RevenueName)]
                public decimal Revenue { get; set; }

                protected override BsonDocument GetExpectedPropertiesBson() {
                    return new BsonDocument {
                        { NameName, Name },
                        { RegistrationDateName, RegistrationDate },
                        { RevenueName, Revenue }
                    };
                }
            }

            [Contract(EventName)]
            public class Relocated : EventBase {
                public const string EventName = "customer_relocated";
                private const string OldAddressName = "old_address";
                private const string NewAddressName = "new_address";

                [ContractMember(OldAddressName)]
                public string OldAddress { get; set; }

                [ContractMember(NewAddressName)]
                public string NewAddress { get; set; }

                public Relocated() : base(ModuleCode, Customer.BsonName, EventName) { }

                protected override BsonDocument GetExpectedPropertiesBson() {
                    return new BsonDocument {
                        { OldAddressName, OldAddress },
                        { NewAddressName, NewAddress }
                    };
                }
            }

            [Contract(EventName)]
            public class Promoted : EventBase {
                public const string EventName = "customer_promoted";

                public Promoted() : base(ModuleCode, Customer.BsonName, EventName) { }
            }
        }

        [ContractContainer(Order.BsonName)]
        public class Order : Aggregate<Order> {
            public const string BsonName = "order";

            public static Order CreateExampleOrder() {
                return new Order()
                    .AddEvent(new Created())
                    .AddEvent(new ProductAdded { ProductID = "PROD1" })
                    .AddEvent(new ProductAdded { ProductID = "PROD2" });
            }

            [Contract(EventName)]
            public class Created : EventBase {
                public const string EventName = "order_created";

                public Created() : base(ModuleCode, Order.BsonName, EventName) { }
            }

            [Contract(EventName)]
            public class ProductAdded : EventBase {
                public const string EventName = "product_added_to_order";
                private const string ProductIDtName = "product_id";

                [ContractMember(ProductIDtName)]
                public string ProductID { get; set; }

                public ProductAdded() : base(ModuleCode, Order.BsonName, EventName) { }

                protected override BsonDocument GetExpectedPropertiesBson() {
                    return new BsonDocument {
                        { ProductIDtName, ProductID }
                    };
                }
            }
        }
    }
}

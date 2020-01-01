using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Testing;
using FluentAssertions;
using MongoDB.Bson;
using System;
using Xbehave;
using Xunit;

namespace EventStore.Mongo {
    [ContractContainer("Test")]
    public partial class EventSerializationFeature : BsonSerializationFeature {
        [Scenario]
        internal void RecordedEvent(
            RecordedEvent rec, 
            long eventID, 
            Guid streamID, 
            DefaultEventMetadata metadata,
            CustomerCreated e,
            RecordedEvent act, 
            BsonDocument doc
        ) {
            GIVEN["a recorded event"] = () => {
                rec = new RecordedEvent(
                    streamID = Guid.NewGuid(),
                    e = new CustomerCreated { Name = "Customer 1" }, 
                    metadata = new DefaultEventMetadata(DateTime.Today));

                rec.ID = new EventID(eventID = 100_000_000_000);
            };

            AND["an appropriate configuration"] = () => ConfigureSerialization();

            WHEN["serializing the event"] = () => doc = Serialize(rec);
            THEN["the BSON is as expected"] = () => {
                var exp = new BsonDocument {
                                        { "_id", new BsonInt64(eventID) },
                                        { "StreamID", new BsonBinaryData(streamID, GuidRepresentation.Standard) },
                                        { "m", new BsonDocument {
                                            { "ts", new BsonDateTime(metadata.Timestamp) },
                                            { "c", BsonNull.Value } } },
                                        { "e", new BsonDocument {
                                            { "_t", "TEST:Test.CustomerCreated" },
                                            { "Name",  e.Name },
                                            { "CreationDate", e.CreationDate } } }
                    };

                doc.Should().BeEqivalentTo(exp);
            };
            
            AND["deserializing it"] = () => act = Deserialize<RecordedEvent>(doc);
            THEN["it has the same value"] = () => act.Should().BeEquivalentTo(rec);
        }

        [Contract]
        internal class CustomerCreated : EventBase {
            public string Name { get; set; }

            public DateTime CreationDate { get; set; } = DateTime.Today;
        }
    }
}
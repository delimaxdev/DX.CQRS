using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.EventStore.Mongo;
using DX.Cqrs.Mongo.Facade;
using DX.Testing;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using System;
using System.Linq;
using Xbehave;
using Xunit.Abstractions;

namespace Integration.Mongo {
    public class MongoFacadeIntegration : MongoFeature {
        public MongoFacadeIntegration(ITestOutputHelper output) : base(output) { }

        [Scenario]
        internal void MaxWithTypedProperty(IMongoFacadeTransaction tx, Maybe<EventID> max) {
            Given["a mongo collection"] = () => Env.DB.CreateCollectionAsync("test");

            USING["a transaction"] = () => {
                ITransaction t = DB.StartTransactionAsync().Result;
                tx = DB.UseTransaction(t);
                return t;
            };
            
            When["calling Max on an empty collection"] = async () => max = await tx
                .GetCollection<TypedPropertyClass>("test")
                .Max(x => x.ID);
            THEN["None<T> is returned"] = () => max.Should().Be(None<EventID>.Value);

            And["a collection with a few items"] = () => tx
                .GetCollection<TypedPropertyClass>("test")
                .InsertManyAsync(new uint[] { 1, 3, 7, 2 }.Select(x => new TypedPropertyClass(id: x)));
            When["calling Max"] = async () => max = await tx
                .GetCollection<TypedPropertyClass>("test")
                .Max(x => x.ID);
            THEN["the maximum value is returned"] = () =>
                max.Value.Serialize().Should().Be(7);
        }

        [Scenario]
        internal void MaxWithUntypedProperty(IMongoFacadeTransaction tx, Maybe<object> max, Maybe<EventID> typedMax) {
            GIVEN["a ClassMap with custom serializer"] = () => BsonClassMap.RegisterClassMap<UntypedPropertyClass>(m => m
                .MapIdField(x => x.ID)
                .SetSerializer(EventIDSerializer.CastInstance));
            Given["a mongo collection"] = () => Env.DB.CreateCollectionAsync("test");
            
            USING["a transaction"] = () => {
                ITransaction t = DB.StartTransactionAsync().Result;
                tx = DB.UseTransaction(t);
                return t;
            };

            When["calling Max on an empty collection"] = async () => max = await tx
                .GetCollection<UntypedPropertyClass>("test")
                .Max(x => x.ID);
            THEN["None<T> is returned"] = () => max.Should().Be(None<Object>.Value);

            And["a collection with a few items"] = () => tx
                .GetCollection<UntypedPropertyClass>("test")
                .InsertManyAsync(new uint[] { 1, 3, 7, 2 }.Select(x => new UntypedPropertyClass(id: x)));

            When["calling Max"] = async () => max = await tx
                .GetCollection<UntypedPropertyClass>("test")
                .Max(x => x.ID);

            THEN["the maximum value is returned"] = () => {
                max.Value.Should().BeOfType<EventID>();
                max.Value.As<EventID>().Serialize().Should().Be(7);
            };

            When["calling Max on a interface-typed collection"] = async () => typedMax = await tx
                .GetCollection<UntypedPropertyClass>("test")
                .Max<EventID>("_id");

            THEN["the maximum value is returned"] = () => {
                typedMax.Value.Should().BeOfType<EventID>();
                typedMax.Value.As<EventID>().Serialize().Should().Be(7);
            };

        }

        private class TypedPropertyClass {
            public TypedPropertyClass(long id) {
                ID = new EventID(id);
            }

            public EventID ID { get; set; }
        }

        private class UntypedPropertyClass : IUntypedPropertyInterface {
            public UntypedPropertyClass(long id) {
                ID = new EventID(id);
            }

            public object ID { get; set; }
        }

        private interface IUntypedPropertyInterface {
            object ID { get; set; }
        }
    }
}

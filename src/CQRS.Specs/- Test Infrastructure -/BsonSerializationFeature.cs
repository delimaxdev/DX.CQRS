using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Contracts.Serialization;
using DX.Cqrs.Commons;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Cqrs.Mongo.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xbehave;
using Xunit;

namespace DX.Testing {
    [Collection("BSON")]
    // By default, xUnit runs all tests in parallel. To avaoid this, we put all BSON dependend tests
    // in the same xUnit Collection so that they are run sequentially!
    // See https://xunit.net/docs/running-tests-in-parallel.html for more info.
    public class BsonSerializationFeature : Feature {
        internal BsonSerializationIsolator SerializationIsolator { get; private set; }

        static BsonSerializationFeature() {
            // HACK: Otherwise MongoEventStoreIntegration.SaveAndGet fails if run alone (without
            // executing other tests). Prabably some issue with MongoDB picking up the Defaults 
            // and caching them before it the GuidRepresentation is changed....
            BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
            ConfigureSerialization2();
        }

        [Background]
        public virtual void Background() {
            //USING["a BSON serialization test isolator"] = () => SerializationIsolator = BsonSerializationIsolator.Isolate();
        }

        protected static void ConfigureSerialization(IBsonSerializer streamIDSerializer = null) { }
            protected static void ConfigureSerialization2(IBsonSerializer streamIDSerializer = null) {
            SerializationTypeRegistry types = new SerializationTypeRegistry();
            ContractTypeSerializerOptions options = new ContractTypeSerializerOptions();
            options.Serializers.Add(new DX.Contracts.Serializers.IDSerializer());
            ContractTypeSerializer contractTypeSerializer = new ContractTypeSerializer(types, options);

            MongoEventStore.ConfigureSerialization(
                new MongoEventStoreSerializatonSettings(
                    streamIDSerializer ?? new GuidSerializer(BsonType.Binary),
                    new ContractTypeSerializerAdapter<object, IEvent>(contractTypeSerializer),
                    new ContractTypeSerializerAdapter<object, DefaultEventMetadata>(contractTypeSerializer),
                    new TypeNameResolver(types)));

        }

        protected static BsonDocument Serialize<TNominal>(TNominal o) {
            return o.ToBsonDocument<TNominal>();
        }

        protected static TNominal Deserialize<TNominal>(BsonDocument doc) {
            return BsonSerializer.Deserialize<TNominal>(doc);
        }

        internal class BsonSerializationIsolator : Disposable {
            private static readonly CompositeIsolator _isolators = new CompositeIsolator() {
                new StaticFieldIsolator(typeof(BsonClassMap), "__classMaps", () => new Dictionary<Type, BsonClassMap>()),
                new StaticFieldIsolator(typeof(BsonSerializer), "__idGenerators", () => new Dictionary<Type, IIdGenerator>()),
                new StaticFieldIsolator(typeof(BsonSerializer), "__discriminatorConventions", () => new Dictionary<Type, IDiscriminatorConvention>()),
                new StaticFieldIsolator(typeof(BsonSerializer), "__discriminators", () => new Dictionary<BsonValue, HashSet<Type>>()),
                new StaticFieldIsolator(typeof(BsonSerializer), "__discriminatedTypes", () => new HashSet<Type>()),
                new StaticFieldIsolator(typeof(BsonSerializer), "__typesWithRegisteredKnownTypes", () => new HashSet<Type>()),
                new InitializerMethodIsolator(typeof(BsonSerializer), "CreateSerializerRegistry") {
                    new StaticFieldIsolator(typeof(BsonSerializer), "__serializerRegistry"),
                    new StaticFieldIsolator(typeof(BsonSerializer), "__typeMappingSerializationProvider")
                },
                new StaticFieldIsolator(typeof(BsonDocumentWriterSettings), "__defaults"),
                new StaticFieldIsolator(typeof(BsonDocumentReaderSettings), "__defaults")
            };

            private Dictionary<object, object> _state = new Dictionary<object, object>();

            private BsonSerializationIsolator() { }

            public static BsonSerializationIsolator Isolate() {
                var i = new BsonSerializationIsolator();
                i.Replace();
                return i;
            }

            public void Restore() {
                _isolators.Restore(_state);
            }

            protected override void Dispose(bool disposing) {
                if (!disposing)
                    return;

                Restore();
            }

            private void Replace() {
                _isolators.Replace(_state);

                // TODO: Where should this code really go??
                // https://stackoverflow.com/questions/43473147/how-to-use-decimal-type-in-mongodb
                BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            }

            private class CompositeIsolator : IEnumerable {
                private readonly List<CompositeIsolator> _children = new List<CompositeIsolator>();

                public CompositeIsolator Add(CompositeIsolator child) {
                    _children.Add(child);
                    return this;
                }

                public virtual void Replace(Dictionary<object, object> state) {
                    _children.ForEach(x => x.Replace(state));
                }

                public virtual void Restore(Dictionary<object, object> state) {
                    _children.ForEach(x => x.Restore(state));
                }

                IEnumerator IEnumerable.GetEnumerator() {
                    return _children.GetEnumerator();
                }
            }


            private class StaticFieldIsolator : CompositeIsolator {
                private readonly FieldInfo _field;
                private readonly Func<object> _valueFactory;

                public StaticFieldIsolator(Type type, string fieldName, Func<object> valueFactory = null) {
                    _field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
                    _valueFactory = valueFactory;
                }


                public override void Replace(Dictionary<object, object> state) {
                    state[this] = _field.GetValue(null);

                    if (_valueFactory != null) {
                        object value = _valueFactory();
                        _field.SetValue(null, value);
                    }
                }

                public override void Restore(Dictionary<object, object> state) {
                    _field.SetValue(null, state[this]);
                }
            }

            private class InitializerMethodIsolator : CompositeIsolator {
                private readonly MethodInfo _initializerMethod;
                private readonly object[] _initializerArgs;

                public InitializerMethodIsolator(Type type, string initializerMethod, params object[] initializerArgs) {
                    _initializerMethod = type
                        .GetMethod(initializerMethod, BindingFlags.NonPublic | BindingFlags.Static);
                    _initializerArgs = initializerArgs;
                }

                public override void Replace(Dictionary<object, object> state) {
                    base.Replace(state);
                    _initializerMethod.Invoke(null, _initializerArgs);
                }
            }
        }
    }
}
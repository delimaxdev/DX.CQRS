using DX.Contracts;
using DX.Contracts.Serialization;
using DX.Cqrs.Mongo.Serialization;
using DX.Testing;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Xbehave;
using Xunit;

[assembly: ContractAssembly("TEST")]
namespace Serialization {
    [ContractContainer("Test")]
    partial class SerializationFeature : BsonSerializationFeature {
        [Scenario]
        internal void ContractTypeSerialization(
            ContractTypeSerializer serializer,
            UpdateCertificationObjects original,
            BsonDocument bson,
            UpdateCertificationObjects deserialized
        ) {
            GIVEN["a contract type serializer"] = () => serializer = CreateSerializer();

            WHEN["serializing a message"] = () => bson = SerializeBson(
                serializer,
                original = new UpdateCertificationObjectsBuilder {
                    Objects = {
                        new CertificationObjectDOBuilder { ID = ID.NewID(), Name = "Object 1" }.Build(),
                        new CertificationObjectDOBuilder { ID = ID.NewID(), Name = "Object 2" }.Build()},
                    Service = new ServiceCode("SERV1324")
                }.Build());
            THEN["the serialized BSON equals the expected"] = () => bson.Should().BeEqivalentTo(original.GetExpectedBson());

            WHEN["deserializing the BSON"] = () => deserialized = DeserializeBson<UpdateCertificationObjects>(serializer, bson);
            THEN["the deserialized BSON equals the original"] = () => deserialized.Should().BeEquivalentTo(original);

            GIVEN["an object where a property of polymorphic contract type is null"] = () => original = new UpdateCertificationObjectsBuilder();
            WHEN["serializing and deserializing it"] = () => {
                bson = SerializeBson(serializer, original);
                deserialized = DeserializeBson<UpdateCertificationObjects>(serializer, bson);
            };
            THEN["its value is still null"] = () => deserialized.Service.Should().BeNull();
        }

        private static ContractTypeSerializer CreateSerializer() {
            return SerializerManager
                .CreateDefault(SerializationTypeRegistry.Default)
                .ContractTypeSerializer;
        }

        private static BsonDocument SerializeBson(ContractTypeSerializer serializer, ICommandMessage message) {
            BsonDocument result = new BsonDocument();
            using var writer = new BsonDocumentWriter(result);
            serializer.Serialize(new MongoWriterAdapter(writer), message);
            return result;
        }

        private static T DeserializeBson<T>(ContractTypeSerializer serializer, BsonDocument bson) {
            using var reader = new BsonDocumentReader(bson);
            return (T)serializer.Deserialize<ICommandMessage>(
                new MongoReaderAdapter(reader));
        }
    }
}
using DX.Contracts;
using DX.Contracts.Serialization;
using DX.Testing;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xbehave;

namespace Serialization {
    public partial class SerializationFeature : Feature {
        [Scenario]
        internal void SerializationTypeRegistryTests(
            SerializationTypeRegistry registry,
            ContractTypeInfo info
        ) {
            GIVEN["a new registry"] = () => registry = new SerializationTypeRegistry();
            WHEN["getting info for discriminator"] = () => info = (ContractTypeInfo)registry.GetInfo("TEST:Test.UpdateObjects");
            THEN["the info contains the correct type"] = () => info.Type.Should().Be<UpdateCertificationObjects>();
            WHEN["getting info for a type"] = () => info = (ContractTypeInfo)registry.GetInfo(typeof(UpdateServices));
            THEN["the info contains expected infos"] = () => {
                info.Name.Name.Should().Be("Test.UpdateServices");
                info.Name.ModuleCode.Should().Be("TEST");
            };
        }

        [Scenario]
        internal void SerializationTypeNameTests(SerializationTypeName name) {
            WHEN["parsing a valid name"] = () => name = SerializationTypeName.Parse("TEST:Test.Sample");
            THEN["the result is correct"] = () => {
                name.ModuleCode.Should().Be("TEST");
                name.Name.Should().Be("Test.Sample");
            };
        }

        [Scenario]
        internal void Subtypes(SerializationTypeRegistry registry, SerializationTypeInfo info) {
            GIVEN["a registry with some types"] = ()
                => registry = new SerializationTypeRegistry(typeof(SerializationFeature).GetNestedTypes(BindingFlags.NonPublic));
            WHEN["getting the type info"] = () => info = registry.GetInfo(typeof(ICommandMessage));
            THEN["it contains all subclasses"] = () => info.Subclasses.Should().BeEquivalentTo(
                registry.GetInfo(typeof(UpdateCertificationObjects)),
                registry.GetInfo(typeof(UpdateServices)));
        }

        [Scenario]
        internal void ContractTypeSerialization(
            ContractTypeSerializer serializer,
            UpdateCertificationObjects original,
            JObject json,
            UpdateCertificationObjects deserialized
        ) {
            GIVEN["a contract type serializer"] = () => serializer = CreateSerializer();

            WHEN["serializing a message"] = () => json = SerializeJson(
                serializer,
                original = new UpdateCertificationObjectsBuilder {
                    Objects = {
                        new CertificationObjectDOBuilder { ID = ID.NewID(), Name = "Object 1", Type = ObjectType.Crop, TypeRef = ID.NewID().ToRef<IObjectType>() }.Build(),
                        new CertificationObjectDOBuilder { ID = ID.NewID(), Name = "Object 2", Type = ObjectType.Livestock, TypeRef = ID.NewID().ToRef<IObjectType>() }.Build() },
                    Service = new ServiceCode("SERV1234")
                }.Build());
            THEN["the serialized JSON equals the expected"] = () => json.Should().BeEquivalentTo(original.GetExpectedJson());

            WHEN["deserializing the JSON"] = () => deserialized = DeserializeJson<UpdateCertificationObjects>(serializer, json);
            THEN["the deserialized JSON equals the original"] = () => deserialized.Should().BeEquivalentTo(original);

            GIVEN["an object where a property of polymorphic contract type is null"] = () => original = new UpdateCertificationObjectsBuilder();
            WHEN["serializing and deserializing it"] = () => {
                json = SerializeJson(serializer, original);
                deserialized = DeserializeJson<UpdateCertificationObjects>(serializer, json);
            };
            THEN["its value is still null"] = () => deserialized.Service.Should().BeNull();
        }

        [Scenario]
        internal void DictionarySerialization(
            ContractTypeSerializer serializer,
            UpdateServices original,
            JObject json,
            UpdateServices deserialized
        ) {
            GIVEN["a contract type serializer"] = () => serializer = CreateSerializer();

            WHEN["serializing a message"] = () => json = SerializeJson(
                serializer,
                original = new UpdateServicesBuilder {
                    Status = {
                        { ID.NewID(),  new StatusDOBuilder { Name = "Status 1" }.Build() },
                        { ID.NewID(),  new StatusDOBuilder { Name = "Status 2" }.Build() } }
                }.Build());
            THEN["the serialized JSON equals the expected"] = () => json.Should().BeEquivalentTo(original.GetExpectedJson());

            WHEN["deserializing the JSON"] = () => deserialized = DeserializeJson<UpdateServices>(serializer, json);
            THEN["the deserialized JSON equals the original"] = () => deserialized.Should().BeEquivalentTo(original);
        }


        [Scenario]
        internal void InvalidContractTypeSerialization(InvalidContractClass value) {
            GIVEN["a contract class that references a non-contract class"] = () => value = new InvalidContractClass();
            THEN["serializing", ThrowsA<ContractTypeSerializationException>()] = () => {
                using JTokenWriter writer = new JTokenWriter();
                CreateSerializer().Serialize(writer, value);
            };
        }

        [Scenario]
        internal void SerializerSettings(
            JsonSerializerSettings settings,
            NonContractClsas original,
            JObject serialized,
            NonContractClsas deserialized
        ) {
            GIVEN["the JsonSerializerSettings from a SerializationManager instance"] = () => {
                settings = new JsonSerializerSettings();
                CreateSerializer().EnableContractTypeResolution(settings);
            };

            WHEN["serializing a class without ContractAttribute"] = () => serialized = JObject.FromObject(
                original = new NonContractClsas {
                    StringValue = "Value",
                    ContractClassProperty = new UpdateCertificationObjectsBuilder().Build()
                },
                JsonSerializer.Create(settings));

            THEN["the ContractTypeSerializer is not used for the non-contract class"] = () =>
                serialized.ContainsKey("StringValue").Should().BeTrue();
            AND["the ContractTypeSerializer is used for the contract-type property"] = () =>
                serialized.Value<JObject>("ContractClassProperty").ContainsKey("_t").Should().BeTrue();

            WHEN["the object is deserialized"] = () => deserialized = serialized.ToObject<NonContractClsas>(JsonSerializer.Create(settings));
            THEN["it is equal to the original"] = () => deserialized.Should().BeEquivalentTo(original);
        }

        [Scenario]
        internal void ContractTypeTests(bool result) {
            WHEN["calling IsPolymorphicContract on a sub class"] = () => result = ContractType.IsPolymorphicContract(typeof(Subclass));
            THEN["attributes on base classes are considered"] = () => result.Should().BeTrue();
        }

        [Contract]
        internal class Subclass : BaseClass { }

        [Contract(IsPolymorphic = true)]
        internal class BaseClass { }

        private static ContractTypeSerializer CreateSerializer() {
            return SerializerManager
                .CreateDefault(SerializationTypeRegistry.Default)
                .ContractTypeSerializer;
        }

        private static JObject SerializeJson(ContractTypeSerializer serializer, ICommandMessage message) {
            using JTokenWriter writer = new JTokenWriter();
            serializer.Serialize(writer, message);
            return (JObject)writer.Token;
        }

        private static T DeserializeJson<T>(ContractTypeSerializer serializer, JObject json) {
            return (T)serializer.Deserialize<ICommandMessage>(json.CreateReader());
        }
    }
}
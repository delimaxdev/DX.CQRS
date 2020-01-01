using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DX.Contracts.Serialization
{
    internal class PolymorphicJsonConverter : RecursionSafeJsonConverter {
        public const string DiscriminatorProperty = "_t";
        private readonly SerializationTypeRegistry _types;

        public PolymorphicJsonConverter(SerializationTypeRegistry types)
            => _types = Check.NotNull(types, nameof(types));

        public override bool CanConvert(Type objectType) => true;
        
        protected override void WriteJsonCore(JsonWriter writer, object value, JsonSerializer serializer) {
            string discriminator = ((ContractTypeInfo)_types.GetInfo(value.GetType())).Discriminator;
            
            if (writer is OptimizedJsonWriter optimizedWriter) {
                WriteJsonFast(optimizedWriter, value, discriminator, serializer);
            } else {
                JObject json = JObject.FromObject(value, serializer);
                json[DiscriminatorProperty] = JToken.FromObject(discriminator);
                writer.WriteToken(json.CreateReader());
            }
        }

        protected override object ReadJsonCore(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            // Also needed by ReadJsonFast (would read null discriminator and throw an
            // exception otherwise).
            if (reader.TokenType == JsonToken.Null) {
                return null!;
            }

            if (reader is OptimizedJsonReader optimizedReader) {
                return ReadJsonFast(optimizedReader, serializer)!;
            } else {
                JObject json = serializer.Deserialize<JObject>(reader);
                string discriminator = json.GetValue(DiscriminatorProperty).Value<string>();
                Type actualType = _types.GetInfo(discriminator).Type;
                return serializer.Deserialize(json.CreateReader(), actualType);
            }
        }

        private void WriteJsonFast(OptimizedJsonWriter writer, object value, string discriminator, JsonSerializer serializer) {
            writer.InjectDiscriminator(DiscriminatorProperty, discriminator);
            serializer.Serialize(writer, value);
        }

        private object? ReadJsonFast(OptimizedJsonReader reader, JsonSerializer serializer) {
            if (reader.ReadDiscriminatorOrNull(DiscriminatorProperty, out string? discriminator)) {
                Type actualType = _types.GetInfo(discriminator.NotNull()).Type;
                return serializer.Deserialize(reader, actualType);
            } else {
                return null;
            }
        }
    }
}

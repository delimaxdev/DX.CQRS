using DX.Contracts.Serialization;
using Newtonsoft.Json;
using System;

namespace DX.Contracts.Serializers {
    public class IDSerializer : CustomSerializer {
        public IDSerializer() : base(typeof(ID)) { }

        public override JsonConverter CreateConverter(Type type, SerializerRegistry registry)
            => IDJsonConverter.Instnace;

        public override string GenerateSchema(Type type, SerializerRegistry registry) => @"{
                ""type"": ""string"",
                ""format"": ""guid""
            }";

        private class IDJsonConverter : JsonConverter {
            public static readonly IDJsonConverter Instnace = new IDJsonConverter();

            public override bool CanConvert(Type objectType)
                => objectType == typeof(ID);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                if (value == null) {
                    writer.WriteNull();
                    return;
                }

                ID id = (ID)value;
                writer.WriteValue(ID.ToGuid(id));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                if (reader.TokenType == JsonToken.Null) {
                    return null!;
                }

                return reader.Value switch
                {
                    Guid guid => ID.FromGuid(guid),
                    string str => ID.Parse(str),
                    _ => throw new NotImplementedException()
                };
            }
        }
    }
}

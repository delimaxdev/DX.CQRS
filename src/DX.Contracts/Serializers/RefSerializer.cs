using DX.Contracts.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts.Serializers {
    public class RefSerializer : GenericSerializer {
        public RefSerializer() : base(typeof(Ref<>)) { }

        protected override JsonConverter CreateGenericConverter<T>(Type type, SerializerRegistry registry) {
            var info = new RefInfo(type, registry);

            Type converterType = typeof(RefConverter<,>)
                .MakeGenericType(info.TargetType, info.IDType);

            return (JsonConverter)Activator.CreateInstance(converterType, info.IDConverter);
        }

        protected override string GenerateGenericSchema<T>(Type type, SerializerRegistry registry) {
            throw new NotImplementedException(
                "We do not use a custom TypeMapper to generate the OpenAPI schema but we directly modify " +
                "the Type of Ref properties via a custom IReflectionService.");
        }

        private class RefInfo {
            public readonly Type TargetType;
            public readonly Type IDType;
            public readonly JsonConverter IDConverter;

            public RefInfo(Type type, SerializerRegistry registry) {
                TargetType = Ref.GetTargetType(type);
                IDType = Ref.GetIdentifierType(TargetType);

                if (!registry.TryGetConverter(IDType, out IDConverter!)) {
                    throw new InvalidOperationException(
                        $"Can not serialize type 'Ref<{TargetType.Name}>'. The given 'SerializerRegistry' " +
                        $"does not contain a serializer for IDs of type '{IDType.Name}'.");
                }
            }
        }

        private class RefConverter<TTarget, TID> : JsonConverter 
            where TID : IIdentifier
            where TTarget : IHasID<TID>, IHasID<IIdentifier> {

            private readonly JsonConverter _idConverter;

            public RefConverter(JsonConverter idConverter)
                => _idConverter = idConverter;

            public override bool CanConvert(Type objectType)
                => true;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                if (value == null) {
                    writer.WriteNull();
                    return;
                }

                Ref<TTarget> r = (Ref<TTarget>)value;
                Ref.ExtractIdentifier<TTarget, TID>(r, out TID id);
                _idConverter.WriteJson(writer, id, serializer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                if (reader.TokenType == JsonToken.Null) {
                    return null!;
                }

                TID id = (TID)_idConverter.ReadJson(reader, typeof(TID), null, serializer);
                return Ref.FromIdentifier<TTarget, TID>(id);
            }
        }
    }
}
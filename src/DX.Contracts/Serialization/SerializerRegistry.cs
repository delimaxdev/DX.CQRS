using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DX.Contracts.Serialization {
    public class SerializerRegistry {
        private static readonly DefaultContractResolver __defaultResolver = new DefaultContractResolver();
        private Dictionary<Type, CustomSerializer> _serializers = new Dictionary<Type, CustomSerializer>();

        public IEnumerable<CustomSerializer> Serializers
            => _serializers.Values;

        public void Add(CustomSerializer serializer) {
            Check.NotNull(serializer, nameof(serializer));
            _serializers.Add(serializer.Type, serializer);
        }

        public bool TryGetConverter(Type type, out JsonConverter? converter) {
            if (TryGetSerializer(type, out CustomSerializer s)) {
                converter = s.CreateConverter(type, this);
                return true;
            }

            converter = default;
            return false;
        }

        internal SerializerRegistry Clone() {
            return new SerializerRegistry {
                _serializers = new Dictionary<Type, CustomSerializer>(_serializers)
            };
        }

        public void ConfigureJsonSettings(JsonSerializerSettings settings) {
            Check.NotNull(settings, nameof(settings));
            settings.ContractResolver = new CustomSerializerResolver(
                settings.ContractResolver ?? __defaultResolver,
                this);
        }

        private bool TryGetSerializer(Type type, out CustomSerializer s) {
            Check.NotNull(type, nameof(type));

            if (_serializers.TryGetValue(type, out s))
                return true;

            return
                type.IsGenericType &&
                _serializers.TryGetValue(type.GetGenericTypeDefinition(), out s);
        }

        private class CustomSerializerResolver : IContractResolver {
            private readonly IContractResolver _actual;
            private readonly SerializerRegistry _serializers;

            public CustomSerializerResolver(IContractResolver actual, SerializerRegistry serializers)
                => (_actual, _serializers) = (actual, serializers);

            public JsonContract ResolveContract(Type type) {
                Type actualType = Nullable.GetUnderlyingType(type) ?? type;
                return _serializers.TryGetConverter(actualType, out JsonConverter? converter) ?
                    new JsonObjectContract(actualType) { Converter = converter } :
                    _actual.ResolveContract(type);
            }
        }
    }
}

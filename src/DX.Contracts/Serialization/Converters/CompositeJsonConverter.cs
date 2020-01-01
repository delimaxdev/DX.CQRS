using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DX.Contracts.Serialization
{
    public class CompositeJsonConverter : JsonConverter {
        private readonly Type _objectType;
        private readonly JsonConverter[] _converters;

        public CompositeJsonConverter(Type objectType, params JsonConverter[] converters) {
            _objectType = Check.NotNull(objectType, nameof(objectType));
            _converters = Check.NotEmpty(converters, nameof(converters));
            Check.Requires(converters.All(c => c.CanConvert(objectType)));
        }

        public static JsonConverter? Create(Type objectType, IEnumerable<JsonConverter> converters) {
            JsonConverter[] cs = converters.ToArray();
            return cs.Length switch
            {
                0 => null,
                1 => cs[0],
                _ => new CompositeJsonConverter(objectType, cs)
            };
        }

        public override bool CanConvert(Type objectType)
            => _objectType.IsAssignableFrom(objectType);

        // See 'GetFirstConverter' for why we must always return true
        public override bool CanRead => true;

        // See 'GetFirstConverter' for why we must always return true
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => GetFirstConverter(c => c.CanRead).ReadJson(reader, objectType, existingValue, serializer);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) 
            => GetFirstConverter(c => c.CanWrite).WriteJson(writer, value, serializer);

        private JsonConverter GetFirstConverter(Func<JsonConverter, bool> predicate) {
            JsonConverter c = _converters.FirstOrDefault(predicate);
            if (c != null)
                return c;

            const string exception = "At least one of the JsonConverter's of a CompositeJsonConverter has to return true for" +
                "CanRead or CanWrite at any time. Reason: The CompositeJsonConverter has to return a value for CanRead/CanRead " +
                "but it can not check the CanRead/CanWrite property of its sub converters because accessing this property may " +
                "change they state of a converter, as is the case with RecursionSafeJsonConverter and converters like the " +
                "JsonInheritanceConverter.";

            throw new InvalidOperationException(exception);
        }
    }
}

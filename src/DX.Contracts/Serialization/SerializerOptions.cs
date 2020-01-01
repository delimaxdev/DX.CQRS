using DX.Cqrs.Commons;
using Newtonsoft.Json;

namespace DX.Contracts.Serialization {
    public class SerializerOptions {
        public SerializerRegistry Serializers { get; }

        public JsonSerializerSettings JsonSettings { get; }

        public SerializerOptions()
            : this(new SerializerRegistry(), new JsonSerializerSettings()) {
        }

        internal SerializerOptions(SerializerRegistry serializers, JsonSerializerSettings jsonSettings) {
            Serializers = Check.NotNull(serializers, nameof(serializers));
            JsonSettings = Check.NotNull(jsonSettings, nameof(jsonSettings));
        }

        internal ContractTypeSerializerOptions Clone()
            => new ContractTypeSerializerOptions(Serializers.Clone(), CloneJsonSettings());

        internal JsonSerializerSettings CloneJsonSettings()
            => ObjectUtils.ShallowCopyTo(JsonSettings, new JsonSerializerSettings());
    }
}
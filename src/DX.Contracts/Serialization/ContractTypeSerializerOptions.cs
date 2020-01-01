using DX.Cqrs.Commons;
using Newtonsoft.Json;

namespace DX.Contracts.Serialization {
    public class ContractTypeSerializerOptions {
        public SerializerRegistry Serializers { get; }

        public JsonSerializerSettings JsonSettings { get; }

        public ContractTypeSerializerOptions()
            : this(new SerializerRegistry(), new JsonSerializerSettings()) {
        }

        internal ContractTypeSerializerOptions(SerializerRegistry serializers, JsonSerializerSettings jsonSettings) {
            Serializers = Check.NotNull(serializers, nameof(serializers));
            JsonSettings = Check.NotNull(jsonSettings, nameof(jsonSettings));
        }

        internal ContractTypeSerializerOptions Clone()
            => new ContractTypeSerializerOptions(Serializers.Clone(), CloneJsonSettings());

        internal JsonSerializerSettings CloneJsonSettings()
            => ObjectUtils.ShallowCopyTo(JsonSettings, new JsonSerializerSettings());
    }
}

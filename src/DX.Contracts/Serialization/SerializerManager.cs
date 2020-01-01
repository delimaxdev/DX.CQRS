using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DX.Contracts.Serialization {
    public class SerializerManager {
        private readonly List<IConfigureSerializers> _setups;

        public SerializerRegistry Serializers { get; } = new SerializerRegistry();

        public ContractTypeSerializer ContractTypeSerializer { get; }

        public SerializerManager(IEnumerable<IConfigureSerializers> setups, SerializationTypeRegistry types) {
            Check.NotNull(setups, nameof(setups));
            Check.NotNull(types, nameof(types));
            _setups = setups.ToList();

            ContractTypeSerializerOptions contractOptions = new ContractTypeSerializerOptions(
                Serializers,
                new JsonSerializerSettings());

            foreach (IConfigureSerializers setup in setups) {
                setup.ConfigureSerializers(Serializers);
                setup.ConfigureContractTypeSerializer(contractOptions.JsonSettings);
            }

            ContractTypeSerializer = new ContractTypeSerializer(types, contractOptions);
        }

        public void ConfigureJsonSerializerSettings(JsonSerializerSettings settings) {
            foreach (IConfigureSerializers setup in _setups)
                setup.ConfigureWebSerializer(settings);

            ContractTypeSerializer.EnableContractTypeResolution(settings);
            Serializers.ConfigureJsonSettings(settings);
        }

        public static SerializerManager CreateDefault(SerializationTypeRegistry types) {
            return new SerializerManager(
                new List<IConfigureSerializers> { new DefaultSerializerSetup() },
                Check.NotNull(types, nameof(types)));
        }

    }
}
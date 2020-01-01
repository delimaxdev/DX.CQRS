using Newtonsoft.Json;

namespace DX.Contracts.Serialization {
    public interface IConfigureSerializers {
        void ConfigureSerializers(SerializerRegistry serializers);

        void ConfigureContractTypeSerializer(JsonSerializerSettings settings);

        void ConfigureWebSerializer(JsonSerializerSettings settings);
    }
}
using DX.Contracts.Serializers;
using Newtonsoft.Json;

namespace DX.Contracts.Serialization {
    public class DefaultSerializerSetup : IConfigureSerializers {
        public virtual void ConfigureContractTypeSerializer(JsonSerializerSettings settings) { }

        public virtual void ConfigureSerializers(SerializerRegistry serializers) {
            serializers.Add(new IDSerializer());
            serializers.Add(new RefSerializer());
        }

        public virtual void ConfigureWebSerializer(JsonSerializerSettings settings) { }
    }
}
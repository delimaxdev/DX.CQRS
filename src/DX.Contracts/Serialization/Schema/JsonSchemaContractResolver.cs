using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace DX.Contracts.Serialization.Schema {
    public class JsonSchemaContractResolver : DefaultContractResolver {
        private readonly SerializerRegistry _serializers;

        public JsonSchemaContractResolver(SerializerRegistry serializers)
            => _serializers = Check.NotNull(serializers, nameof(serializers));

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            property.PropertyName = ContractType.GetMemberName(member);
            return property;
        }
    }
}
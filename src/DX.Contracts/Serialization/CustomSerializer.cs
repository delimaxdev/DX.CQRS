using Newtonsoft.Json;
using System;

namespace DX.Contracts.Serialization {
    public abstract class CustomSerializer {
        protected CustomSerializer(Type type)
            => Type = Check.NotNull(type, nameof(type));

        public Type Type { get; }

        public abstract JsonConverter CreateConverter(Type type, SerializerRegistry registry);

        public abstract string GenerateSchema(Type type, SerializerRegistry registry);
    }
}

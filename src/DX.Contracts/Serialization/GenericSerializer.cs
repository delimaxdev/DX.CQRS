using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace DX.Contracts.Serialization {
    public abstract class GenericSerializer : CustomSerializer {
        private static readonly MethodInfo __createGenericConverterMethod = typeof(GenericSerializer)
            .GetMethod(nameof(CreateGenericConverter), BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo __generateGenericSchemaMethod = typeof(GenericSerializer)
            .GetMethod(nameof(GenerateGenericSchema), BindingFlags.NonPublic | BindingFlags.Instance);
        
        public GenericSerializer(Type genericTypeDefinition) : base(genericTypeDefinition) {
            Check.Requires(genericTypeDefinition.IsGenericTypeDefinition, nameof(genericTypeDefinition));
        }

        public override JsonConverter CreateConverter(Type type, SerializerRegistry registry)
            => (JsonConverter)InvokeGenericMethod(__createGenericConverterMethod, type, registry);

        public override string GenerateSchema(Type type, SerializerRegistry registry)
            => (string)InvokeGenericMethod(__generateGenericSchemaMethod, type, registry);
        
        protected abstract JsonConverter CreateGenericConverter<T>(Type type, SerializerRegistry registry);

        protected abstract string GenerateGenericSchema<T>(Type type, SerializerRegistry registry);

        private object InvokeGenericMethod(MethodInfo method, Type type, SerializerRegistry registry) {
            MethodInfo m = method
                .MakeGenericMethod(type.GetGenericArguments()
                .First());

            return m.Invoke(this, new object[] { type, registry });
        }
    }
}
using DX.Contracts.Serialization;
using DX.Cqrs.Commons;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DX.Cqrs.Serialization.Schema {
    internal class InheritanceSchemaProcessor : ISchemaProcessor {
        private readonly SerializationTypeRegistry _types;

        public InheritanceSchemaProcessor(SerializationTypeRegistry types)
            => _types = Check.NotNull(types, nameof(types));

        public void Process(SchemaProcessorContext context) {
            if (context.Type.IsGenericType) {
                foreach (Type genericArgument in context.Type.GenericTypeArguments) {
                    GetOrGenerate(genericArgument, context);
                }
            }

            if (_types.TryGetInfo(context.Type, out SerializationTypeInfo i) && i is ContractTypeInfo contractInfo) {
                context.Schema.ExtensionData = new Dictionary<string, object> {
                    ["x-ms-discriminator-value"] = contractInfo.Discriminator
                };
            }

            if (!ContractType.IsPolymorphicContract(context.Type))
                return;

            var info = (ContractTypeInfo)_types
                .GetInfo(context.Type);

            if (info.Type.IsAbstract || info.Type.IsInterface) {
                context.Schema.Discriminator = "_t";
                context.Schema.Properties.Add(
                    "_t",
                    new JsonSchemaProperty {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    });
            } else {
                foreach (Type interfaceType in ReflectionUtils.GetAllInterfaces(context.Type)) {
                    if (_types.TryGetInfo(interfaceType, out _)) {
                        JsonSchema interfaceSchema = GetOrGenerate(interfaceType, context);
                        context.Schema.AllOf.Add(
                            new JsonSchema { Reference = interfaceSchema });
                    }
                }
            }

            foreach (ContractTypeInfo subclass in info.Subclasses.OfType<ContractTypeInfo>()) {
                JsonSchema s = GetOrGenerate(subclass.Type, context);
            }
        }

        private static JsonSchema GetOrGenerate(Type type, SchemaProcessorContext context) {
            if (context.Resolver.HasSchema(type, false))
                return context.Resolver.GetSchema(type, false);

            return context.Generator.Generate(type, context.Resolver);
        }
    }
}
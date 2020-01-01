using DX.Contracts;
using DX.Contracts.Serialization;
using DX.Contracts.Serialization.Schema;
using DX.Cqrs.Serialization.Schema;
using Microsoft.Extensions.Options;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NSwag.Generation.AspNetCore;
using System;

namespace DX.Web.Serialization {
    internal class OpenApiDocumentGeneratorSettingsSetup : IConfigureOptions<AspNetCoreOpenApiDocumentGeneratorSettings> {
        private readonly SerializerManager _manager;
        private readonly SerializationTypeRegistry _types;

        public OpenApiDocumentGeneratorSettingsSetup(SerializerManager manager, SerializationTypeRegistry types) {
            _manager = Check.NotNull(manager, nameof(manager));
            _types = Check.NotNull(types, nameof(types));
        }

        public void Configure(AspNetCoreOpenApiDocumentGeneratorSettings options) {
            options.SerializerSettings ??= new JsonSerializerSettings();
            options.SerializerSettings.ContractResolver = new JsonSchemaContractResolver(_manager.Serializers);
            options.SerializerSettings.Converters.Add(new StringEnumConverter(
                new DefaultNamingStrategy(), allowIntegerValues: false));
            
            // NSwag updates actual properties only when this property is set,
            // without this statement, a NullReferenceException is thrown.
            options.SerializerSettings = options.SerializerSettings;

            foreach (CustomSerializer s in _manager.Serializers.Serializers) {
                options.TypeMappers.Add(new CustomTypeMapper(s, _manager.Serializers));
            }

            options.SchemaProcessors.Add(new InheritanceSchemaProcessor(_types));
            options.SchemaNameGenerator = new SchemaNameGenerator(_types);
            options.FlattenInheritanceHierarchy = true;
            options.ReflectionService = new CustomReflectionService();
            options.PostProcess = d => {
                // Neccessary to avoid "Cannot delete property" exception when using AutoRest 3
                // (https://github.com/Azure/autorest/issues/3253)
                d.Generator = null;
            };
        }
        
        private class CustomReflectionService : DefaultReflectionService {
            public override JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings) {
                if (Ref.IsRefType(contextualType.Type)) {
                    Type targetType = Ref.GetTargetType(contextualType.Type);
                    Type idType = Ref.GetIdentifierType(targetType);
                    return base.GetDescription(idType.ToContextualType(), defaultReferenceTypeNullHandling, settings);
                }

                return base.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings);
            }
        }


        private class CustomTypeMapper : ITypeMapper {
            private readonly CustomSerializer _serializer;
            private readonly SerializerRegistry _registry;

            public CustomTypeMapper(CustomSerializer serializer, SerializerRegistry registry)
                => (_serializer, _registry) = (serializer, registry);

            public Type MappedType => _serializer.Type;

            public bool UseReference => false;

            public void GenerateSchema(JsonSchema schema, TypeMapperContext context) {
                string schemaJson = _serializer.GenerateSchema(context.Type, _registry);
                JsonConvert.PopulateObject(schemaJson, schema);
            }
        }

        private class SchemaNameGenerator : DefaultSchemaNameGenerator {
            private readonly SerializationTypeRegistry _types;

            public SchemaNameGenerator(SerializationTypeRegistry types)
                => _types = types;

            public override string Generate(Type type) {
                if (_types.TryGetInfo(type, out SerializationTypeInfo info) && info is ContractTypeInfo contractInfo) {
                    return contractInfo.Name.Name.Replace('.', '_');
                }

                return base.Generate(type);
            }
        }
    }
}
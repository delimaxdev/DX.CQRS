using DX.Contracts;
using DX.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Converters;
using NJsonSchema.Generation;
using System.Runtime.Serialization;
using Xbehave;
using Xunit.Abstractions;

namespace Serialization
{
    public class JsonSchemaGenerationFeature : Feature {
        private readonly ITestOutputHelper _output;

        public JsonSchemaGenerationFeature(ITestOutputHelper output) {
            _output = output;
        }

        [Scenario]
        internal void GenerateSchema(JObject schema) {
            WHEN["generating a schema"] = () => {
                schema = Generate<ICustomerCommand>();
                _output.WriteLine(schema.ToString());
            };

        }

        private static JObject Generate<T>() {
            JsonSchemaGeneratorSettings settings = new JsonSchemaGeneratorSettings();
            settings.SchemaType = SchemaType.OpenApi3;
            settings.GenerateKnownTypes = true;

            JsonSchema schema = JsonSchema.FromType<T>();
            return JObject.Parse(schema.ToJson());
        }

        private class CreateCustomer {
            public string Name { get; set; }
        }

        private class AddADdressToCustomer {
            public string Address { get; set; }
        }

        [JsonConverter(typeof(JsonInheritanceConverter))]
        [KnownType(typeof(CreateCustomer))]
        [KnownType(typeof(AddADdressToCustomer))]
        private abstract class ICustomerCommand : ICommandMessage {
            public string ID { get; set; }
        }
    }
}

using DX.Cqrs.Commons;
using DX.Testing;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xbehave;

namespace Common
{
    public class ObjectUtilsFeature : Feature {
        [Scenario]
        internal void ShallowCopy(object source, object target) {
            GIVEN["an two objects"] = () => {
                source = new TestObject {
                    Title = "Source",
                    Object = new TestObject { Title = "Object" },
                    Collection = { new TestObject { Title = "Item 1" }, new TestObject { Title = "Item 2" } }
                };

                target = new TestObject {
                    Title = "Target",
                    Collection = { new TestObject { Title = "Target Item" } }
                };
            };

            WHEN["copying"] = () => ObjectUtils.ShallowCopyTo(source, target);
            THEN["the objects are equivalent"] = () => target.Should().BeEquivalentTo(source);

            WHEN["copying JSON settings"] = () => ObjectUtils.ShallowCopyTo(
                source = new JsonSerializerSettings { Converters = { new TestConverter() } },
                target = new JsonSerializerSettings());

            THEN["the objects are equivalent"] = () => target.Should().BeEquivalentTo(source, o => o.ExcludingFields());
        }

        private class TestConverter : JsonConverter {
            public override bool CanConvert(Type objectTyp) => false;
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => null;
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
        }

        private class TestObject {
            public Guid Value { get; set; } = Guid.NewGuid();

            public string Title { get; set; }

            public object Readonly { get; } = "DEFAULT";

            public List<TestObject> Collection { get; } = new List<TestObject>();

            public TestObject Object { get; set; }
        }
    }
}

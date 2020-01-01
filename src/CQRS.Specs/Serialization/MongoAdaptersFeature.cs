using DX.Cqrs.Mongo.Serialization;
using DX.Testing;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbehave;

namespace Serialization {
    public class MongoAdaptersFeature : BsonSerializationFeature {

        [Scenario]
        internal void Serialization(SampleClass original, BsonDocument doc, SampleClass deserialized) {
            GIVEN["a sample value"] = () => {
                original = new SampleClass {
                    StringValue = "Test",
                    DateTimeValue = new DateTime(2019, 09, 30, 20, 30, 30, DateTimeKind.Local),
                    StringArray = new[] { "Item 1", "Item 2" },
                    ClassValue = new SampleClass { StringValue = "Test" },
                    DecimalValue = 0.1m,
                    GuidValue = Guid.NewGuid(),
                    UriValue = new Uri("https://www.google.com/")
                };
                original.ClassList.Add(new SampleClass { StringValue = "Item 1" });
                original.ClassList.Add(new SampleClass { StringValue = "Item 2" });
            };

            WHEN["serializing"] = () => doc = Serialize(original);
            THEN["it results in expected BSON"] = () => doc.Should().BeEqivalentTo(original.GetExpectedBson());

            WHEN["deserializing"] = () => deserialized = Deserialize<SampleClass>(doc);
            THEN["it equals the original object"] = () => deserialized.Should().BeEquivalentTo(original);
        }

        private static BsonDocument Serialize(object value) {
            BsonDocument result = new BsonDocument();

            MongoWriterAdapter adapter = new MongoWriterAdapter(
                new BsonDocumentWriter(result)
            );

            new JsonSerializer().Serialize(adapter, value);
            return result;
        }

        private static T Deserialize<T>(BsonDocument document) {
            MongoReaderAdapter adapter = new MongoReaderAdapter(new BsonDocumentReader(document));
            return new JsonSerializer().Deserialize<T>(adapter);
        }


        public class SampleClass {
            public string StringValue { get; set; }

            public DateTime? DateTimeValue { get; set; }

            public DateTime? DateTimeValueNull { get; set; }

            public decimal DecimalValue { get; set; }

            public string[] StringArray { get; set; }

            public SampleClass ClassValue { get; set; }

            public List<SampleClass> ClassList { get; } = new List<SampleClass>();

            public Guid GuidValue { get; set; }

            public Uri UriValue { get; set; }

            public BsonDocument GetExpectedBson()
                => new BsonDocument {
                    { nameof(StringValue), StringValue },
                    { nameof(DateTimeValue), DateTimeValue },
                    { nameof(DateTimeValueNull), DateTimeValueNull },
                    { nameof(DecimalValue), DecimalValue },
                    { nameof(StringArray), GetArrayOrNull(StringArray) },
                    { nameof(ClassValue), GetDocumentOrNull(ClassValue) },
                    { nameof(ClassList), GetArrayOrNull(ClassList) },
                    { nameof(GuidValue), new BsonBinaryData(GuidValue, GuidRepresentation.Standard) },
                    { nameof(UriValue), GetValueOrNull(UriValue) }
                };

            private static BsonValue GetValueOrNull(Uri uri) {
                if (uri == null)
                    return BsonNull.Value;
                return uri.OriginalString;
            }

            private static BsonValue GetArrayOrNull<T>(T[] value) {
                if (value == null)
                    return BsonNull.Value;
                return new BsonArray(value);
            }

            private static BsonValue GetArrayOrNull(IEnumerable<SampleClass> value) {
                if (value == null)
                    return BsonNull.Value;

                return new BsonArray(value.Select(x => x.GetExpectedBson()));
            }

            private static BsonValue GetDocumentOrNull(SampleClass value) {
                if (value == null)
                    return BsonNull.Value;
                return value.GetExpectedBson();
            }
        }
    }
}

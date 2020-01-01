using DX.Contracts.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Globalization;

namespace DX.Cqrs.Mongo.Serialization {
    public class MongoWriterAdapter : OptimizedJsonWriter {
        private static readonly RepresentationConverter _converter = new RepresentationConverter(
            allowOverflow: false,
            allowTruncation: false);

        private readonly IBsonWriter _bson;
        private (string Name, string Value)? _nextDiscriminator;

        public MongoWriterAdapter(IBsonWriter bsonWriter)
            => _bson = Check.NotNull(bsonWriter, nameof(bsonWriter));

        public override void InjectDiscriminator(string name, string value) =>
            _nextDiscriminator = (
                Check.NotEmpty(name, nameof(name)),
                Check.NotEmpty(value, nameof(value)));

        public override void WriteStartObject() {
            _bson.WriteStartDocument();

            if (_nextDiscriminator != null) {
                _bson.WriteString(
                    _nextDiscriminator.Value.Name,
                    _nextDiscriminator.Value.Value
                );

                _nextDiscriminator = null;
            }
        }

        public override void WriteEndObject()
            => _bson.WriteEndDocument();

        public override void WriteStartArray()
            => _bson.WriteStartArray();

        public override void WriteEndArray()
            => _bson.WriteEndArray();

        public override void WritePropertyName(string name)
            => _bson.WriteName(name);

        public override void WriteNull()
            => _bson.WriteNull();

        public override void WriteValue(bool value)
            => _bson.WriteBoolean(value);

        public override void WriteValue(byte value)
            => _bson.WriteInt32(value);

        public override void WriteValue(char value)
            => _bson.WriteInt32(value);

        public override void WriteValue(decimal value)
            => _bson.WriteDecimal128(value);

        public override void WriteValue(double value)
            => _bson.WriteDouble(value);

        public override void WriteValue(float value)
            => _bson.WriteDouble(value);

        public override void WriteValue(int value)
            => _bson.WriteInt32(value);

        public override void WriteValue(long value)
            => _bson.WriteInt64(value);

        public override void WriteValue(sbyte value)
            => _bson.WriteInt32(value);

        public override void WriteValue(short value)
            => _bson.WriteInt32(value);

        public override void WriteValue(string value)
            => _bson.WriteString(value);

        public override void WriteValue(uint value)
            => _bson.WriteInt32(_converter.ToInt32(value));

        public override void WriteValue(ulong value)
            => _bson.WriteInt64(_converter.ToInt64(value));

        public override void WriteValue(ushort value)
            => _bson.WriteInt32(_converter.ToInt32(value));

        public override void WriteValue(byte[] value)
            => _bson.WriteBytes(value);

        public override void WriteValue(DateTime value)
            => _bson.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(value.ToUniversalTime()));

        public override void WriteValue(Guid value)
            => _bson.WriteBinaryData(new BsonBinaryData(value, GuidRepresentation.Standard));

        public override void WriteValue(DateTimeOffset value)
            => _bson.WriteString(value.ToString(null, CultureInfo.InvariantCulture));

        public override void WriteValue(TimeSpan value)
            => _bson.WriteString(value.ToString(null, CultureInfo.InvariantCulture));

        public override void WriteValue(Uri value)
            => _bson.WriteString(value.OriginalString);

        public override void Flush()
            => _bson.Flush();
    }
}

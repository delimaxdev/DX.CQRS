using DX.Cqrs.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

#pragma warning disable CS8603 // Possible null reference return.

namespace DX.Cqrs.EventStore.Mongo {
    internal class EventIDSerializer : SerializerBase<EventID> {
        public static readonly EventIDSerializer Instance = new EventIDSerializer();
        public static readonly IBsonSerializer<Object> CastInstance = new CastSerializer<Object>(Instance);

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventID value) {
            if (value == null) {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteInt64(value.Serialize());
        }

        public override EventID Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
            IBsonReader r = context.Reader;

            if (r.CurrentBsonType == BsonType.Null) {
                r.ReadNull();
                return null;
            }

            EnsureBsonTypeEquals(r, BsonType.Int64);
            long rawValue = r.ReadInt64();

            return new EventID(rawValue);
        }
    }
}
using DX.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace DX.Cqrs.Mongo.Serialization {
    public class IDSerializer : SerializerBase<ID> {
        private readonly GuidSerializer _guidSerializer = new GuidSerializer(BsonType.Binary);

        public IDSerializer() { }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ID value) {
            if (value == null) {
                context.Writer.WriteNull();
                return;
            }

            _guidSerializer.Serialize(context, args, ID.ToGuid(value));
        }

        public override ID Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
            if (context.Reader.CurrentBsonType == BsonType.Null) {
                context.Reader.ReadNull();
                return null!;
            }

            Guid value = _guidSerializer.Deserialize(context, args);
            return ID.FromGuid(value);
        }
    }
}
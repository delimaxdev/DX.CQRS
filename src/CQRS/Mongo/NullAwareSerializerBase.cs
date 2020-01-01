using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DX.Cqrs.Mongo {
    public abstract class NullAwareSerializerBase<T> : SerializerBase<T> where T : class {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value) {
            if (value == null) {
                context.Writer.WriteNull();
                return;
            }

            SerializeCore(context.Writer, value);
        }

        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
            IBsonReader r = context.Reader;

            if (r.CurrentBsonType == BsonType.Null) {
                r.ReadNull();
                return null!;
            }

            return DeserializeCore(r);
        }

        protected abstract void SerializeCore(IBsonWriter writer, T value);

        protected abstract T DeserializeCore(IBsonReader reader);
    }
}

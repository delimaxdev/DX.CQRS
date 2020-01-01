using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DX.Cqrs.Mongo
{
    public class CastSerializer<T> : SerializerBase<T> {
        private readonly IBsonSerializer _actual;

        public CastSerializer(IBsonSerializer actual)
            => _actual = Check.NotNull(actual, nameof(actual));

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value) {
            if (value == null) {
                context.Writer.WriteNull();
                return;
            }

            _actual.Serialize(context, args, value);
        }

        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
            IBsonReader r = context.Reader;

            if (r.CurrentBsonType == BsonType.Null) {
                r.ReadNull();
                return default;
            }

            return (T)(object)_actual.Deserialize(context, args)!;
        }
    }
}

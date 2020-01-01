using DX.Contracts.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DX.Cqrs.Mongo.Serialization {
    public class ContractTypeSerializerAdapter<TProperty, TValue> : SerializerBase<TProperty> where TValue : TProperty {
        private readonly ContractTypeSerializer _serializer;

        public ContractTypeSerializerAdapter(ContractTypeSerializer serializer)
            => _serializer = Check.NotNull(serializer, nameof(serializer));

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TProperty value) {
            MongoWriterAdapter writer = new MongoWriterAdapter(context.Writer);
            _serializer.Serialize<TValue>(writer, (TValue)value!);
        }

        public override TProperty Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
            MongoReaderAdapter reader = new MongoReaderAdapter(context.Reader);
            return _serializer.Deserialize<TValue>(reader);
        }
    }
}

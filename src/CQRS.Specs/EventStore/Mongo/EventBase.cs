using DX.Contracts;
using MongoDB.Bson;

namespace EventStore.Mongo {
    public class EventBase : IEvent {
        private readonly string _bsonName;

        public EventBase(string bsonName = null) {
            _bsonName = bsonName;
        }

        public EventBase(string moduleCode, string containerName, string contractName) {
            _bsonName = $"{moduleCode}:{containerName}.{contractName}";
        }

        public BsonDocument GetExpectedBson() {
            BsonDocument commonProperties = new BsonDocument {
                { "_t", _bsonName }
            };

            return commonProperties.Merge(GetExpectedPropertiesBson());
        }

        protected virtual BsonDocument GetExpectedPropertiesBson() {
            return new BsonDocument();
        }
    }
}

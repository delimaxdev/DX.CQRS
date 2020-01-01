using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mongo
{
    partial class MongoFake {
        internal class FakeStore {
            private ImmutableDictionary<string, ImmutableList<BsonDocument>> _docs;

            public FakeStore() {
                _docs = ImmutableDictionary<string, ImmutableList<BsonDocument>>.Empty;
            }

            private FakeStore(FakeStore other) {
                _docs = other._docs;
            }

            public IReadOnlyCollection<string> GetCollectionNames() {
                return _docs.Keys.ToArray();
            }

            public void Insert(string collection, params BsonDocument[] docs) {
                Modify(collection, c => c.AddRange(docs));
            }

            public void CreateCollection(string name) {
                var c = ImmutableList<BsonDocument>.Empty;
                _docs = _docs.Add(name, c);
            }

            public IEnumerable<BsonDocument> Query(string collectionName)
                => GetCollection(collectionName);

            public FakeStore Copy() {
                return new FakeStore(this);
            }

            public void Modify(string collection, Func<ImmutableList<BsonDocument>, ImmutableList<BsonDocument>> mutation) {
                var updated = mutation(GetCollection(collection));
                _docs = _docs.SetItem(collection, updated);
            }

            private ImmutableList<BsonDocument> GetCollection(string name) {
                if (!_docs.TryGetValue(name, out var c)) {
                    CreateCollection(name);
                    return _docs[name];
                }

                return c;
            }
        }
    }
}

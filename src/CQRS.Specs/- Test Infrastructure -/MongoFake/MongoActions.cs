using MongoDB.Bson;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Mongo
{
    internal partial class MongoActions {
        public abstract class CollectionAction : MongoAction {
            public CollectionAction(string collectionName)
                => CollectionName = collectionName;

            public string CollectionName { get; }
        }
    }


    partial class MongoActions {
        public class Transaction : MongoAction {
            public Transaction(MongoActionLog log)
                => Log = log;

            public MongoActionLog Log { get; }
        }

        public class InsertMany<T> : CollectionAction {
            public InsertMany(string collectionName, T[] documents) : base(collectionName) {
                Documents = documents
                    .Select(x => x.ToBsonDocument())
                    .ToArray();
            }

            public BsonDocument[] Documents { get; }

            public override void Execute(MongoFake.FakeStore store) {
                store.Insert(CollectionName, Documents);
            }
        }

        public class Upsert<T> : CollectionAction {
            public Upsert(string collectionName, object id, T document) : base(collectionName) {
                ID = id;
                Document = document.ToBsonDocument();
            }

            Object ID { get; }

            BsonDocument Document { get; }

            public override void Execute(MongoFake.FakeStore store) {
                store.Modify(CollectionName, DoUpsert);
            }

            private ImmutableList<BsonDocument> DoUpsert(ImmutableList<BsonDocument> collection) {
                int index = collection
                    .FindIndex(x => ID.Equals(x.GetValue("_id")));

                if (index == -1) {
                    return collection.Add(Document);
                } else {
                    return collection.SetItem(index, Document);
                }
            }
        }

        public class CreateCollectionAction : MongoAction {
            public string Name { get; set; }

            public override void Execute(MongoFake.FakeStore store) {
                store.CreateCollection(Name);
            }

            public override string ToString()
                => $"CREATE COLLECTION {Name}";
        }

        public class CreateIndexAction : CollectionAction {
            public string ElementName { get; set; }

            public CreateIndexAction(string collectionName, string elementName) : base(collectionName) {
                ElementName = elementName;
            }

            public override string ToString()
                => $"CREATE INDEX ON {CollectionName}.{ElementName}";

        }

        public class CommitTransactionAction : MongoAction { }

        public class AbortTransactionAction : MongoAction { }
    }
}

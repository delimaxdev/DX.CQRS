namespace Mongo
{
    internal abstract class MongoActionFluentInterface<TReturn> {
        public TReturn CreateCollection(string name)
            => OnCreate(new MongoActions.CreateCollectionAction { Name = name });

        public TReturn CreateIndex(string collection, string elementName)
            => OnCreate(new MongoActions.CreateIndexAction(collection, elementName));

        public TReturn InsertMany<T>(string collection, params T[] items)
            => OnCreate(new MongoActions.InsertMany<T>(collection, items));

        public TReturn Upsert<T>(string collection, object id, T item)
            => OnCreate(new MongoActions.Upsert<T>(collection, id, item));

        public TReturn CommitTransaction()
            => OnCreate(new MongoActions.CommitTransactionAction());

        public TReturn AbortTransaction()
            => OnCreate(new MongoActions.AbortTransactionAction());


        protected abstract TReturn OnCreate(MongoAction action);
    }
}

using DX.Cqrs.Mongo.Facade;

namespace Mongo
{
    partial class MongoFake {
        private class Transactional : IMongoFacadeTransaction {
            private readonly TransactionFake _transaction;

            public Transactional(TransactionFake transaction)
                => _transaction = transaction;

            public ITransactionalMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name) {
                return new CollectionFake<TDocument>(_transaction, name);
            }
        }
    }
}

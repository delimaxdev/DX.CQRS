using DX.Cqrs.Common;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade {
    public partial class MongoFacade : IMongoFacade {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _db;

        public MongoFacade(IMongoClient client, string databaseName) {
            _client = client;
            _db = client.GetDatabase(databaseName);
        }

        public Task CreateCollectionAsync(string name) {
            return _db.CreateCollectionAsync(name);
        }

        public IMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name) {
            IMongoCollection<TDocument> c = _db.GetCollection<TDocument>(name);
            return new CollectionFacade<TDocument>(c);
        }

        public async Task<IReadOnlyCollection<string>> GetCollectionNamesAsync() {
            IAsyncCursor<string> cursor = await _db.ListCollectionNamesAsync();
            return await cursor.ToListAsync();
        }

        public async Task<ITransaction> StartTransactionAsync() {
            IClientSessionHandle session = await _client.StartSessionAsync();
            session.StartTransaction();
            return new MongoTransaction(session);
        }

        public IMongoFacadeTransaction UseTransaction(ITransaction transaction) {
            MongoTransaction tx = Check.IsOfType<MongoTransaction>(transaction, nameof(transaction));
            return new TransactionalFacade(tx.Session, _db);
        }

        class TransactionalFacade : IMongoFacadeTransaction {
            private readonly IClientSessionHandle _session;
            private readonly IMongoDatabase _db;

            public TransactionalFacade(IClientSessionHandle session, IMongoDatabase db)
                => (_session, _db) = (session, db);

            public ITransactionalMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name) {
                IMongoCollection<TDocument> c = _db.GetCollection<TDocument>(name);
                return new TransactionalCollectionFacade<TDocument>(_session, c);
            }
        }
    }
}
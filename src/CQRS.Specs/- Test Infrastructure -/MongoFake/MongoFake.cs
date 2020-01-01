using DX;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Mongo.Facade;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mongo {
    internal partial class MongoFake : IMongoFacade, MongoFake.IActionDispatcher {

        public MongoActionLog Log { get; } = new MongoActionLog();

        public FakeStore Store { get; } = new FakeStore();

        public int BatchSize { get; set; } = 2;

        public Task CreateCollectionAsync(string name) {
            MongoAction a = MongoActionFactory.Default.CreateCollection(name);
            Dispatch(a);
            return Task.CompletedTask;
        }


        public IMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name)
            => new CollectionFake<TDocument>(this, name);

        public Task<IReadOnlyCollection<string>> GetCollectionNamesAsync() {
            return Task.FromResult(Store.GetCollectionNames());
        }

        public Task<ITransaction> StartTransactionAsync() {
            var log = new MongoActionLog();
            Log.Add(new MongoActions.Transaction(log));
            ITransaction tx = new TransactionFake(this, log);
            return Task.FromResult(tx);
        }

        public IMongoFacadeTransaction UseTransaction(ITransaction transaction) {
            TransactionFake tx = Check.IsOfType<TransactionFake>(transaction, nameof(transaction));
            return new Transactional(tx);
        }

        public void Dispatch(MongoAction writeAction) {
            Log.Add(writeAction);
            writeAction.Execute(Store);
        }

        public TResult Dispatch<TResult>(Func<FakeStore, TResult> query) {
            return query(Store);
        }

        private class TransactionFake : Disposable, ITransaction, IActionDispatcher {
            private readonly MongoFake _db;
            private readonly MongoActionLog _log;
            private bool _isActive = true;

            public TransactionFake(MongoFake db, MongoActionLog log)
                => (_db, _log) = (db, log);

            public Task AbortAsync() {
                CheckAccess();
                _isActive = false;

                _log.AbortTransaction();
                return Task.CompletedTask;
            }

            public Task CommitAsync() {
                CheckAccess();
                _isActive = false;

                CommitTo(_db.Store);
                _log.CommitTransaction();

                return Task.CompletedTask;
            }

            public void Dispatch(MongoAction writeAction) {
                CheckAccess();
                _log.Add(writeAction);
            }

            public TResult Dispatch<TResult>(Func<FakeStore, TResult> query) {
                FakeStore current = _db.Store.Copy();
                CommitTo(current);
                return query(current);
            }

            protected override void Dispose(bool disposing) {
                if (_isActive) {
                    AbortAsync();
                }
            }

            private void CheckAccess() {
                ThrowIfDisposed();
                if (!_isActive) {
                    throw new InvalidOperationException();
                }
            }

            private void CommitTo(FakeStore s) {
                _log.Actions.ForEach(a => a.Execute(s));
            }
        }
    }

    partial class MongoFake {
        private interface IActionDispatcher {
            void Dispatch(MongoAction writeAction);

            TResult Dispatch<TResult>(Func<FakeStore, TResult> query);
        }
    }
}

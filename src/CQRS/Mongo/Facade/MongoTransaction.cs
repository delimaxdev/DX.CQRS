using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade {
    public class MongoTransaction : Disposable, ITransaction {
        public IClientSessionHandle Session { get; }

        public MongoTransaction(IClientSessionHandle session) 
            => Session = Check.NotNull(session, nameof(session));

        public Task AbortAsync() 
            => Session.AbortTransactionAsync();

        public async Task CommitAsync() { 
            await Session.CommitTransactionAsync();
            Session.StartTransaction();
        }

        protected override void Dispose(bool disposing) {
            if (disposing)
                Session.Dispose();
        }
    }
}
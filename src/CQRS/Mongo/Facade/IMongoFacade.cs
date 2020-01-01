using DX.Cqrs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade
{
    public interface IMongoFacade {
        Task<IReadOnlyCollection<string>> GetCollectionNamesAsync();

        Task CreateCollectionAsync(string name);

        IMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name);

        //Task<ITransactionalMongoFacade> StartTransactionAsync();

        IMongoFacadeTransaction UseTransaction(ITransaction transaction);

        Task<ITransaction> StartTransactionAsync();
    }

    public interface IMongoFacadeTransaction {
        ITransactionalMongoCollectionFacade<TDocument> GetCollection<TDocument>(string name);
    }
}
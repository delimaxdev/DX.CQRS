using DX.Contracts.ReadModels;
using DX.Cqrs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public interface IQuery<TCriteria, TResult>
        where TCriteria : ICriteria<TResult> {

        Task<TResult> Run(TCriteria criteria, IContext context);
    }

    public interface ICollectionQuery<TCriteria, TItem> :
        IQuery<TCriteria, IReadOnlyCollection<TItem>>
        where TCriteria : ICollectionCriteria<TItem> { }
}
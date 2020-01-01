using System.Collections.Generic;

namespace DX.Contracts.ReadModels
{
    public interface ICriteria<TResult> : 
        ICriteria { }

    public interface ICollectionCriteria<TItem> : 
        ICriteria<IReadOnlyCollection<TItem>> { }
    
    [Contract(IsPolymorphic = true)]
    public interface ICriteria {

    }
}
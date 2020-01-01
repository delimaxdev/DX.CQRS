using DX.Cqrs.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DX.Cqrs.EventStore
{
    public interface IEventStore {
        IEventStoreTransaction UseTransaction(ITransaction transaction);

        IEventCriteria CreateCriteria(Action<IEventCriteriaBuilder> builder);
    }

    public interface IEventStoreTransaction {
        Task Save<T>(EventBatch<T> s);

        Task<bool> Exists<T>(StreamLocator<T> s);

        Task<IAsyncEnumerable<RecordedEvent>> Get<T>(StreamLocator<T> s);

        Task<IAsyncEnumerable<RecordedEvent>> Get(IEventCriteria criteria);

        Task<IAsyncEnumerable<EventBatch>> GetAll(IEventCriteria? criteria = null);
    }

    public interface IEventCriteriaBuilder {
        void Type<TEvent>();

        void Stream(object streamID);
    }

    public interface IEventCriteria {

    }

    public interface ITypeNameResolver {
        string GetTypeName(Type type);
    }
}

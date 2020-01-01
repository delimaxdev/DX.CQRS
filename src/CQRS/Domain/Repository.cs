using DX.Contracts;
using DX.Cqrs.Domain.Core;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DX.Cqrs.Domain {
    public class Repository<T> : IRepository<T> where T : class, IPersistable {
        private readonly IEventStoreTransaction _transaction;
        private readonly IEventMetadataProvider _metadataProvider;

        public Repository(IEventStoreTransaction transaction, IEventMetadataProvider metadataProvider) {
            _transaction = Check.NotNull(transaction, nameof(transaction));
            _metadataProvider = Check.NotNull(metadataProvider, nameof(metadataProvider));
        }

        public async Task Save(T @object) {
            Check.NotNull<ArgumentException>(@object.ID, "The 'ID' of 'object' cannot be null.");

            Changeset cs = @object.GetChanges();

            bool isNewInStore = !await _transaction.Exists(new StreamLocator<T>(@object.ID));

            if (cs.IsNew != isNewInStore)
                throw new InvalidOperationException("An object with the same ID already exists!");

            RecordedEvent[] changes = cs
                .Changes
                .Select(e => new RecordedEvent(@object.ID, e, _metadataProvider.Provide()))
                .ToArray();

            await _transaction.Save(new EventBatch<T>(@object.ID, changes));
            @object.ClearChanges();
        }

        public Task<bool> Exists(ID id) {
            return _transaction.Exists(new StreamLocator<T>(id));
        }

        public async Task<T?> TryGet(ID id) {
            IAsyncEnumerable<RecordedEvent> asyncEvents = await _transaction.Get(new StreamLocator<T>(id));
            List<RecordedEvent> events = await asyncEvents.ToList();

            if (events.Any()) {
                T obj = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
                obj.Restore(id, events.Select(x => (IEvent)x.Event));
                return obj;
            } else {
                return null;
            }
        }
    }
}

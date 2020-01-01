using DX.Collections;
using DX.Contracts;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public abstract class CollectionQuery<TItem> :
        CollectionQuery<TItem, ID, Nothing>
        where TItem : IHasID<ID> {
        
        public CollectionQuery(IEventStore store)
            : base(store) { }

        protected async Task<IReadOnlyCollection<TItem>> Run(IContext context) {
            var state = await Run(new CollectionQueryState<TItem, ID, Nothing>(Nothing.Value), context);
            return state.Result();
        }
    }

    public abstract class CollectionQuery<TItem, TID, TCriteria> :
        Query<CollectionQueryState<TItem, TID, TCriteria>>
        where TItem : IHasID<TID>
        where TID : IIdentifier {

        public CollectionQuery(IEventStore store) : base(store) { }

        protected async Task<IReadOnlyCollection<TItem>> Run(TCriteria criteria, IContext context) {
            var state = await Run(new CollectionQueryState<TItem, TID, TCriteria>(criteria), context);
            return state.Result();
        }
    }

    public class CollectionQueryState<TItem> : CollectionQueryState<TItem, ID, Nothing>
        where TItem : IHasID<ID> {
       
        public CollectionQueryState() : base(Nothing.Value) { }
    }


    public class CollectionQueryState<TItem, TID, TCriteria>
        where TID : IIdentifier
        where TItem : IHasID<TID> {
        
        private readonly KeyedCollection<TID, TItem> _items = new KeyedCollection<TID, TItem>(x => x.ID);

        public TCriteria Criteria { get; }

        public CollectionQueryState(TCriteria criteria) {
            Criteria = criteria;
        }

        public void Add(TItem item)
            => _items.Add(item);

        public void Remove(TID id)
            => _items.RemoveKey(id);

        public void Update(TID id, Action<TItem> updateAction) {
            Check.NotNull(updateAction, nameof(updateAction));
            updateAction(_items[id]);
        }

        public void Update(Func<TItem, bool> filter, Action<TItem> updateAction) {
            Check.NotNull(filter, nameof(filter));
            Check.NotNull(updateAction, nameof(updateAction));
            updateAction(_items.Single(filter));
        }

        public bool TryUpdate(TID id, Action<TItem> updateAction) {
            Check.NotNull(updateAction, nameof(updateAction));
            if (_items.TryGetValue(id, out TItem item)) {
                updateAction(item);
                return true;
            }
            return false;
        }

        public IReadOnlyCollection<TItem> Result()
            => _items;
    }

    public static class CollectionQueryExtensions {
        public static void Update<TItem, TCriteria, T>(
            this CollectionQueryState<TItem, ID, TCriteria> s, 
            Ref<T> item, 
            Action<TItem> updateAction
        ) where TItem : IHasID<ID> where T : IHasID<ID> {
            Check.NotNull(item, nameof(item));
            s.Update(ID.FromRef(item), updateAction);
        }
    }
}
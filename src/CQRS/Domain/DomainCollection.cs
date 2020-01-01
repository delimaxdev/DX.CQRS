using DX.Collections;
using DX.Contracts;
using System;

namespace DX.Cqrs.Domain
{
    public static class DomainCollection {
        public static IKeyed<TKey, TItem> Create<TKey, TItem>(
            this IKeyed<TKey, TItem>? template,
            Messenger messenger,
            Func<TItem, TKey> keySelector,
            Action<Configurator<IKeyedCollectionMutator<TKey, TItem>>> config
         ) where TKey : notnull {
            var collection = new KeyedCollection<TKey, TItem>(keySelector);
            var configurator = new Configurator<IKeyedCollectionMutator<TKey, TItem>>(
                messenger,
                new KeyedCollectionMutator<TKey, TItem>(collection));
            config(configurator);
            return collection;
        }

        public static IKeyedLookup<TKey, TItem> Create<TKey, TItem>(
            this IKeyedLookup<TKey, TItem>? template,
            Messenger messenger,
            Action<Configurator<ILookupMutator<TKey, TItem>>> config
        ) where TKey : notnull {
            var collection = new KeyedLookup<TKey, TItem>();
            var configurator = new Configurator<ILookupMutator<TKey, TItem>>(
                messenger,
                new KeyedLookupMutator<TKey, TItem>(collection));
            config(configurator);
            return collection;
        }


        public class Configurator<TMutator> {
            private readonly Messenger _m;
            private readonly TMutator _mutator;

            internal Configurator(Messenger messenger, TMutator mutator) {
                _m = messenger;
                _mutator = mutator;
            }

            public void Apply<TEvent>(Action<TEvent, TMutator> action) where TEvent : IEvent {
                Check.NotNull(action, nameof(action));
                _m.Apply<TEvent>(e => action(e, _mutator));
            }


        }

        private abstract class KeyedMutatorBase<TKey, TItem> : IKeyedMutator<TKey, TItem> where TKey : notnull {
            private readonly KeyedCollectionBase<TKey, TItem> _collection;

            protected KeyedMutatorBase(KeyedCollectionBase<TKey, TItem> collection)
                => _collection = collection;

            public TItem this[TKey key] => _collection[key];

            public abstract void Add(TKey key, TItem item);

            public void AddOrModify(TKey key, Action<TItem> modification, Func<TItem> addition) {
                Check.NotNull(key, nameof(key));
                Check.NotNull(modification, nameof(modification));
                Check.NotNull(addition, nameof(addition));

                if (_collection.TryGetValue(key, out TItem item)) {
                    modification(item);
                } else {
                    item = addition();
                    Add(key, item);
                }
            }

            public bool Remove(TKey key)
                => _collection.RemoveKey(key);

            public void Set(TKey key, TItem value)
                => _collection[key] = value;

            public void TryUpdate(TKey key, Action<TItem> update) {
                if (_collection.TryGetValue(key, out TItem item))
                    update(item);
            }
        }


        
        private class KeyedCollectionMutator<TKey, TItem> : KeyedMutatorBase<TKey, TItem>, IKeyedCollectionMutator<TKey, TItem> where TKey : notnull {
            private readonly KeyedCollection<TKey, TItem> _collection;

            public KeyedCollectionMutator(KeyedCollection<TKey, TItem> collection) : base(collection)
                => _collection = collection;

            public void Add(TItem item)
                => _collection.Add(item);

            public override void Add(TKey key, TItem item) 
                => Add(item);
        }

        private class KeyedLookupMutator<TKey, TItem> : KeyedMutatorBase<TKey, TItem>, ILookupMutator<TKey, TItem> where TKey : notnull {
            private readonly KeyedLookup<TKey, TItem> _collection;

            public KeyedLookupMutator(KeyedLookup<TKey, TItem> collection) : base(collection)
                => _collection = collection;

            public override void Add(TKey key, TItem item)
                => _collection.Add(key, item);
        }
    }

    public interface IKeyedCollectionMutator<TKey, TItem> : IKeyedMutator<TKey, TItem> {
        void Add(TItem item);

    }

    public interface ILookupMutator<TKey, TItem> : IKeyedMutator<TKey, TItem> {
        void Add(TKey key, TItem item);

    }

    public interface IKeyedMutator<TKey, TItem> {
        TItem this[TKey key] { get; }
        void AddOrModify(TKey key, Action<TItem> modification, Func<TItem> addition);
        bool Remove(TKey key);
        void Set(TKey key, TItem value);
        void TryUpdate(TKey key, Action<TItem> update);
    }
}

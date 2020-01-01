using System;
using System.Collections;
using System.Collections.Generic;

namespace DX.Collections
{
    public class KeyedCollection<TKey, TItem> : KeyedCollectionBase<TKey, TItem>, ICollection<TItem> where TKey : notnull {
        private readonly Func<TItem, TKey> _keySelector;

        public KeyedCollection(Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null) 
            : base(keyComparer) {
            _keySelector = Check.NotNull(keySelector, nameof(keySelector));
        }

        public KeyedCollection(Func<TItem, TKey> keySelector, IEnumerable<TItem> items) : this(keySelector) {
            Check.NotNull(items, nameof(items));
            AddRange(items);
        }

        public void Add(TItem item) {
            Items.Add(_keySelector(item), item);
        }

        public void AddRange(IEnumerable<TItem> source) {
            foreach (TItem item in source) {
                Add(item);
            }
        }

        public bool Remove(TItem item) {
            return Items.Remove(_keySelector(item));
        }

        void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex) {
            Items.Values.CopyTo(array, arrayIndex);
        }
    }

    public class KeyedLookup<TKey, TItem> : KeyedCollectionBase<TKey, TItem>, IKeyedLookup<TKey, TItem>, IDictionary<TKey, TItem> where TKey : notnull {
        private ICollection<KeyValuePair<TKey, TItem>> ItemsCollection => Items;

        ICollection<TKey> IDictionary<TKey, TItem>.Keys => Items.Keys;

        public ICollection<TItem> Values => Items.Values;

        public IEnumerable<KeyValuePair<TKey, TItem>> Pairs => Items;

        public void Add(TKey key, TItem item) {
            Items.Add(key, item);
        }

        public void AddRange(IKeyedLookup<TKey, TItem> source) {
            foreach (KeyValuePair<TKey, TItem> pair in source.Pairs) {
                Add(pair.Key, pair.Value);
            }
        }

        public new IKeyedLookup<TKey, TItem> ToImmutable() {
            // TODO: Implement!
            return this;
        }

        void ICollection<KeyValuePair<TKey, TItem>>.Add(KeyValuePair<TKey, TItem> item)
            => ItemsCollection.Add(item);

        bool ICollection<KeyValuePair<TKey, TItem>>.Contains(KeyValuePair<TKey, TItem> item)
            => ItemsCollection.Contains(item);

        void ICollection<KeyValuePair<TKey, TItem>>.CopyTo(KeyValuePair<TKey, TItem>[] array, int arrayIndex)
            => ItemsCollection.CopyTo(array, arrayIndex);

        public new IEnumerator<KeyValuePair<TKey, TItem>> GetEnumerator()
            => ItemsCollection.GetEnumerator();

        bool IDictionary<TKey, TItem>.Remove(TKey key)
            => Items.Remove(key);

        bool ICollection<KeyValuePair<TKey, TItem>>.Remove(KeyValuePair<TKey, TItem> item)
            => ItemsCollection.Remove(item);
    }

    public class KeyedCollectionBase<TKey, TItem> : IKeyed<TKey, TItem> where TKey : notnull {
        protected Dictionary<TKey, TItem> Items { get; }

        public KeyedCollectionBase(IEqualityComparer<TKey>? keyComparer = null) 
            : this(new Dictionary<TKey, TItem>(keyComparer)) { }

        private KeyedCollectionBase(Dictionary<TKey, TItem> items)
            => Items = items;

        public TItem this[TKey key] {
            get => Items[key];
            set => Items[key] = value;
        }

        public int Count => Items.Count;

        public bool IsReadOnly => false;

        public IEnumerable<TKey> Keys => Items.Keys;

        public bool TryGetValue(TKey key, out TItem value)
            => Items.TryGetValue(key, out value);

        public bool ContainsKey(TKey key)
            => Items.ContainsKey(key);

        public bool Contains(TItem item)
            => Items.ContainsValue(item);

        public void Clear()
            => Items.Clear();

        public bool RemoveKey(TKey key)
            => Items.Remove(key);

        public IEnumerator<TItem> GetEnumerator()
            => Items.Values.GetEnumerator();

        public IKeyed<TKey, TItem> ToImmutable() {
            // TODO: Implement!
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        public IReadOnlyDictionary<TKey, TItem> AsDictionary()
            => new DictionaryAdapter(Items);

        private class DictionaryAdapter : KeyedCollectionBase<TKey, TItem>, IReadOnlyDictionary<TKey, TItem> {
            public DictionaryAdapter(Dictionary<TKey, TItem> items) 
                : base(items) { }

            public IEnumerable<TItem> Values => Items.Values;

            IEnumerator<KeyValuePair<TKey, TItem>> IEnumerable<KeyValuePair<TKey, TItem>>.GetEnumerator()
                => Items.GetEnumerator();
        }
    }
}

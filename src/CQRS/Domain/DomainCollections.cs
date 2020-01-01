//namespace DX.Cqrs.Domain {
//    using System;
//    using System.Collections.Generic;
//    using System.Text;

//    public class CollectionByKey<TKey, TValue> : CollectionByKeyBase<TKey, TValue> {
//        public void Add(TKey key, TValue value) {
//            AddCore(key, value);
//        }
//    }

//    public class CollectionWithKey<TKey, TValue> : CollectionByKeyBase<TKey, TValue> {
//        private readonly Func<TValue, TKey> _keySelector;

//        public CollectionWithKey(Func<TValue, TKey> keySelector) {
//            _keySelector = Check.NotNull(keySelector, nameof(keySelector));
//        }

//        public void Add(TValue item) {
//            TKey key = _keySelector(item);
//            AddCore(key, item);
//        }
//    }

//    public class DomainCollectionByKey<TKey, TValue> :
//        CollectionByKey<TKey, TValue>,
//        IKeyedDomainCollection<TKey>
//        where TValue : DomainObject {

//        private readonly RootReference _root = new RootReference();

//        IEventRoot IKeyedDomainCollection<TKey>.Root {
//            set => _root.Set(value);
//        }

//        IEventTarget IKeyedDomainCollection<TKey>.GetTarget(TKey key)
//            => this[key];

//        protected override void AddCore(TKey key, TValue value) {
//            base.AddCore(key, value);
//            IEventTarget target = value;
//            target.Root = _root;
//        }
//    }

//    public class DomainCollectionWithKey<TKey, TValue> :
//        CollectionWithKey<TKey, TValue>,
//        IKeyedDomainCollection<TKey>
//        where TValue : DomainObject {

//        private readonly RootReference _root = new RootReference();

//        IEventRoot IKeyedDomainCollection<TKey>.Root {
//            set => _root.Set(value);
//        }

//        IEventTarget IKeyedDomainCollection<TKey>.GetTarget(TKey key)
//            => this[key];

//        public DomainCollectionWithKey(Func<TValue, TKey> keySelector)
//            : base(keySelector) { }

//        protected override void AddCore(TKey key, TValue value) {
//            base.AddCore(key, value);
//            IEventTarget target = value;
//            target.Root = _root;
//        }
//    }

//    public abstract class CollectionByKeyBase<TKey, TValue> {
//        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();

//        public TValue this[TKey key] {
//            get => _items[key];
//            set => _items[key] = value;
//        }

//        public bool Contains(TKey key)
//            => _items.ContainsKey(key);

//        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) {
//            TValue value;

//            if (!_items.TryGetValue(key, out value)) {
//                value = valueFactory(key);
//                AddCore(key, value);
//            }

//            return value;
//        }

//        protected virtual void AddCore(TKey key, TValue value) {
//            Check.NotNull(value, nameof(value));
//            _items.Add(key, value);
//        }
//    }

//    public interface IKeyedDomainCollection<in TKey> {
//        IEventRoot Root { set; }

//        IEventTarget GetTarget(TKey key);
//    }
//}

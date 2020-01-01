using System;
using System.Collections.Generic;

namespace DX.Cqrs.Commons {
    public class Diffset {
        public static Diffset<TKey, TValue> Create<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> original,
            IReadOnlyDictionary<TKey, TValue> modified,
            IEqualityComparer<TValue>? valueComparer = null
        ) where TKey : notnull {
            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            Diffset<TKey, TValue> diff = new Diffset<TKey, TValue>();

            HashSet<TKey> originalKeys = new HashSet<TKey>(original.Keys);
            foreach (KeyValuePair<TKey, TValue> mod in modified) {
                bool foundInOriginal = originalKeys.Remove(mod.Key);
                if (foundInOriginal) {
                    TValue ov = original[mod.Key];
                    if (valueComparer.Equals(mod.Value, ov))
                        diff.Unchanged(mod.Key, ov, mod.Value);
                    else
                        diff.Changed(mod.Key, ov, mod.Value);
                } else {
                    diff.Added(mod.Key, mod.Value);
                }
            }

            foreach (TKey removedKey in originalKeys) {
                TValue removedValue = original[removedKey];
                diff.Removed(removedKey, removedValue);
            }

            return diff;
        }
    }

    public class Diffset<TKey, TValue> : Diffset {
        private readonly List<Change> _changes = new List<Change>();

        public void Apply(Action<ApplyActions> configure) {
            Check.NotNull(configure, nameof(configure));

            ApplyActions actions = new ApplyActions();
            configure(actions);

            foreach (Change change in _changes)
                actions.Apply(change);
        }

        internal void Added(TKey key, TValue newValue)
            => Add(ChangeType.Added, key, default, newValue);

        internal void Removed(TKey key, TValue oldValue)
            => Add(ChangeType.Removed, key, oldValue, default);

        internal void Changed(TKey key, TValue oldValue, TValue newValue)
            => Add(ChangeType.Changed, key, oldValue, newValue);

        internal void Unchanged(TKey key, TValue oldValue, TValue newValue)
            => Add(ChangeType.Unchanged, key, oldValue, newValue);

        private void Add(ChangeType type, TKey key, TValue oldValue, TValue newValue)
            => _changes.Add(new Change(type, key, oldValue, newValue));

        public class ApplyActions {
            private Action<TKey, TValue> _addedAction = delegate { };
            private Action<TKey, TValue> _removedAction = delegate { };
            private Action<TKey, TValue> _unchangedAction = delegate { };
            private Action<TKey, TValue, TValue> _changedAction = delegate { };
            private Action<TKey, TValue, TValue> _matchedAction = delegate { };


            public void Added(Action<TKey, TValue> action)
                => _addedAction = Check.NotNull(action, nameof(action));

            public void Removed(Action<TKey, TValue> action)
                => _removedAction = Check.NotNull(action, nameof(action));

            public void Unchanged(Action<TKey, TValue> action)
                => _unchangedAction = Check.NotNull(action, nameof(action));

            public void Changed(Action<TKey, TValue, TValue> action)
                => _changedAction = Check.NotNull(action, nameof(action));

            public void Matched(Action<TKey, TValue, TValue> action)
                => _matchedAction = Check.NotNull(action, nameof(action));

            internal void Apply(Change c) {
                switch (c.Type) {
                    case ChangeType.Unchanged:
                        _matchedAction(c.Key, c.OldValue, c.NewValue);
                        _unchangedAction(c.Key, c.OldValue);
                        break;
                    case ChangeType.Changed:
                        _matchedAction(c.Key, c.OldValue, c.NewValue);
                        _changedAction(c.Key, c.OldValue, c.NewValue);
                        break;
                    case ChangeType.Added:
                        _addedAction(c.Key, c.NewValue);
                        break;
                    case ChangeType.Removed:
                        _removedAction(c.Key, c.OldValue);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal class Change {
            public ChangeType Type { get; }

            public TKey Key { get; }

            public TValue OldValue { get; }

            public TValue NewValue { get; }

            public Change(ChangeType type, TKey key, TValue oldValue, TValue newValue)
                => (Type, Key, OldValue, NewValue) = (type, key, oldValue, newValue);
        }

        internal enum ChangeType {
            Unchanged,
            Changed,
            Added,
            Removed
        }
    }
}
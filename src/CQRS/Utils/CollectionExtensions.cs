using DX.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DX {
    public static class CollectionExtensions {
        public static bool IsEquivalentTo<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T>? comparer = null) {
            Check.NotNull(first, nameof(first));
            Check.NotNull(second, nameof(second));
            return first.ToHashSet(comparer ?? EqualityComparer<T>.Default).SetEquals(second);
        }


        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> collection, Func<T, IEnumerable<T>> childrenSelector) {
            Check.NotNull(collection, nameof(collection));
            Check.NotNull(childrenSelector, nameof(childrenSelector));
            return collection.SelectMany(x => new[] { x }.Concat(childrenSelector(x).Flatten(childrenSelector)));
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action) {
            Check.NotNull(collection, nameof(collection));
            Check.NotNull(action, nameof(action));

            foreach (T item in collection) {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T, int> action) {
            Check.NotNull(collection, nameof(collection));
            Check.NotNull(action, nameof(action));

            int i = 0;
            foreach (T item in collection) {
                action(item, i++);
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory) {
            Check.NotNull(dict, nameof(dict));
            Check.NotNull(key, nameof(key));
            Check.NotNull(valueFactory, nameof(valueFactory));

            if (!dict.TryGetValue(key, out TValue value)) {
                value = valueFactory(key);
                dict.Add(key, value);
            }

            return value;
        }

        public static TValue GetValueWithDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull {
            return dictionary.TryGetValue(key, out TValue value) ?
                value :
                defaultValue;
        }

        public static TValue GetValueWithDefault<TKey, TValue>(this IKeyed<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull {
            return dictionary.TryGetValue(key, out TValue value) ?
                value :
                defaultValue;
        }
    }
}
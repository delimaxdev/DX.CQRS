using DX.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts {
    public static class ImmutabilityExtensions {
        public static void Mutate<TKey, TItem>(
            this KeyedCollectionBase<TKey, TItem> collection, 
            TKey key, 
            Func<TItem, TItem> mutation
        ) where TKey : notnull {
            Check.NotNull(collection, nameof(collection));
            Check.NotNull(key, nameof(key));
            Check.NotNull(mutation, nameof(mutation));

            collection[key] = mutation(collection[key]);
        }

        public static void Mutate<T>(
            this List<T> collection,
            int index,
            Func<T, T> mutation
        )  {
            Check.NotNull(collection, nameof(collection));
            Check.NotNull(mutation, nameof(mutation));

            collection[index] = mutation(collection[index]);
        }
    }
}
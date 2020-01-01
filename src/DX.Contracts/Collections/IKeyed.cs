using System.Collections.Generic;

namespace DX.Collections
{
    public interface IKeyed<TItem> : IReadOnlyCollection<TItem> {

    }

    public interface IKeyed<TKey, TItem> : IKeyed<TItem> where TKey : notnull {
        IEnumerable<TKey> Keys { get; }

        TItem this[TKey key] { get; }

        bool TryGetValue(TKey key, out TItem value);
        
        IKeyed<TKey, TItem> ToImmutable();

        IReadOnlyDictionary<TKey, TItem> AsDictionary();
    }
    
    public interface IKeyedLookup<TKey, TItem> : IKeyed<TKey, TItem> where TKey : notnull {
        IEnumerable<KeyValuePair<TKey, TItem>> Pairs { get; }

        new IKeyedLookup<TKey, TItem> ToImmutable();
    }
}
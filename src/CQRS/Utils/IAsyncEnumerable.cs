using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DX.Cqrs {
    ///// <summary>Exposes an enumerator that provides asynchronous iteration over values of a specified type.</summary>
    ///// <typeparam name="T">The type of values to enumerate.</typeparam>
    //public interface IAsyncEnumerable<out T> {
    //    /// <summary>Returns an enumerator that iterates asynchronously through the collection.</summary>
    //    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that may be used to cancel the asynchronous iteration.</param>
    //    /// <returns>An enumerator that can be used to iterate asynchronously through the collection.</returns>
    //    IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    //}

    ///// <summary>Supports a simple asynchronous iteration over a generic collection.</summary>
    ///// <typeparam name="T">The type of objects to enumerate.</typeparam>
    //public interface IAsyncEnumerator<out T> : IAsyncDisposable {
    //    /// <summary>Advances the enumerator asynchronously to the next element of the collection.</summary>
    //    /// <returns>
    //    /// A <see cref="ValueTask{Boolean}"/> that will complete with a result of <c>true</c> if the enumerator
    //    /// was successfully advanced to the next element, or <c>false</c> if the enumerator has passed the end
    //    /// of the collection.
    //    /// </returns>
    //    ValueTask<bool> MoveNextAsync();

    //    /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
    //    T Current { get; }
    //}

    ///// <summary>Provides a mechanism for releasing unmanaged resources asynchronously.</summary>
    //public interface IAsyncDisposable {
    //    /// <summary>
    //    /// Performs application-defined tasks associated with freeing, releasing, or
    //    /// resetting unmanaged resources asynchronously.
    //    /// </summary>
    //    ValueTask DisposeAsync();
    //}

    public static class IAsyncEnumerableExtensions {
        public static async Task ForEach<T>(this IAsyncEnumerable<T> collection, Func<T, Task> action) {
            IAsyncEnumerator<T> enumerator = collection.GetAsyncEnumerator();
            try {
                while (await enumerator.MoveNextAsync()) {
                    await action(enumerator.Current);
                }
            } finally {
                await enumerator.DisposeAsync();
            }            
        }

        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> collection) {
            IAsyncEnumerator<T> enumerator = collection.GetAsyncEnumerator();
            try {
                List<T> list = new List<T>();
                
                while (await enumerator.MoveNextAsync()) {
                    list.Add(enumerator.Current);
                }

                return list;
            } finally {
                await enumerator.DisposeAsync();
            }
        }

        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> collection) {
            return new EnumerableWrapper<T>(collection);
        }

        private class EnumerableWrapper<T> : IAsyncEnumerable<T> {
            private readonly IEnumerable<T> _inner;

            public EnumerableWrapper(IEnumerable<T> inner)
                => _inner = inner;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new EnumeratorWrapper<T>(_inner.GetEnumerator());
        }

        private class EnumeratorWrapper<T> : IAsyncEnumerator<T> {
            private readonly IEnumerator<T> _inner;

            public EnumeratorWrapper(IEnumerator<T> inner)
                => _inner = inner;

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
                => new ValueTask();

            public ValueTask<bool> MoveNextAsync()
                => new ValueTask<bool>(_inner.MoveNext());
        }
    }
}

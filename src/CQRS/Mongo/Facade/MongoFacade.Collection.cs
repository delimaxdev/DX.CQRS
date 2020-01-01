using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade
{
    partial class MongoFacade {
        class CollectionFacade<T> : IMongoCollectionFacade<T> {
            protected IMongoCollection<T> Collection { get; }

            public CollectionFacade(IMongoCollection<T> collection)
                => Collection = collection;

            public Task CreateIndex(string elementName) {
                IndexKeysDefinition<T> keys = Builders<T>.IndexKeys.Ascending(elementName);
                CreateIndexModel<T> index = new CreateIndexModel<T>(keys);
                return Collection.Indexes.CreateOneAsync(index);
            }
        }
    }

    partial class MongoFacade {
        private class AsyncCursorEnumerable<T> : IAsyncEnumerable<T> {
            private readonly IAsyncCursor<T> _cursor;

            public AsyncCursorEnumerable(IAsyncCursor<T> cursor)
                => _cursor = cursor;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new AsyncCursorEnumerator<T>(_cursor);
        }

        private class AsyncCursorEnumerator<T> : IAsyncEnumerator<T> {
            private readonly IAsyncCursor<T> _cursor;
            private IEnumerator<T> _currentBatch = Enumerable.Empty<T>().GetEnumerator();

            public AsyncCursorEnumerator(IAsyncCursor<T> cursor)
                => _cursor = cursor;

            public T Current => _currentBatch.Current;

            public ValueTask DisposeAsync() {
                _cursor.Dispose();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync() {
                if (_currentBatch.MoveNext())
                    return true;

                if (await _cursor.MoveNextAsync()) {
                    _currentBatch = _cursor.Current.GetEnumerator();
                    return _currentBatch.MoveNext();
                }

                return false;
            }
        }
    }
}

using DX.Cqrs;
using DX.Cqrs.Commons;
using DX.Cqrs.Mongo.Facade;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mongo {
    partial class MongoFake {
        private class CollectionFake<T> : IMongoCollectionFacade<T>, ITransactionalMongoCollectionFacade<T> {
            private readonly IActionDispatcher _dispatcher;
            private readonly string _name;

            public CollectionFake(IActionDispatcher dispatcher, string name) =>
                (_dispatcher, _name) = (dispatcher, name);

            public Task CreateIndex(string elementName) {
                _dispatcher.Dispatch(MongoActionFactory.Default.CreateIndex(_name, elementName));
                return Task.CompletedTask;
            }

            public Task<IAsyncEnumerable<T>> FindAll<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value) {
                if (propertySelector.Body is MemberExpression exp) {
                    Func<BsonDocument, BsonValue> accessor = GetAccessor(exp);

                    // HACK: Is there a better way? BsonValue.Create() works just for built-in types.
                    BsonValue bsonValue = value.ToBsonDocument().GetValue("_v");
                    T[] result = _dispatcher.Dispatch(s => s
                        .Query(_name)
                        .Where(d => accessor(d).CompareTo(bsonValue) == 0)
                        .Select(d => BsonSerializer.Deserialize<T>(d))
                        .ToArray()
                    );

                    return Task.FromResult(result.ToAsyncEnumerable());

                } else {
                    throw new ArgumentException("Unsupported property expression.", nameof(propertySelector));
                }
            }

            private Func<BsonDocument, BsonValue> GetAccessor(MemberExpression exp) {
                string propertyName = exp.Member.Name;
                Func<BsonDocument, BsonValue> accessor = d => d.GetValue(propertyName, BsonNull.Value);

                while (exp.Expression is MemberExpression) {
                    accessor = prependAccessor(accessor, exp.Member.Name);
                    exp = exp.Expression as MemberExpression;
                }

                return accessor;

                Func<BsonDocument, BsonValue> prependAccessor(Func<BsonDocument, BsonValue> rightAccessor, string elementName) {
                    return d => {
                        BsonDocument nested = d
                            .GetValue(elementName, new BsonDocument())
                            .AsBsonDocument;

                        return rightAccessor(nested);
                    };
                }
            }

            public Task<IAsyncEnumerable<T>> FindAll(
                FilterDefinition<T> filter = null,
                SortDefinition<T> sort = null
            ) {
                // HACK: We silently ignore the sort here
                if ((filter != null && !filter.Equals(Builders<T>.Filter.Empty))) {
                    throw new NotImplementedException();
                }

                T[] result = _dispatcher.Dispatch(s => s
                    .Query(_name)
                    .Select(d => BsonSerializer.Deserialize<T>(d))
                    .ToArray()
                );

                return Task.FromResult(result.ToAsyncEnumerable());
            }

            public Task InsertManyAsync(IEnumerable<T> documents) {
                _dispatcher.Dispatch(MongoActionFactory.Default.InsertMany<T>(_name, documents.ToArray()));
                return Task.CompletedTask;
            }

            public Task<Maybe<TValue>> Max<TValue>(Expression<Func<T, TValue>> propertySelector) {
                throw new NotImplementedException();
            }

            public Task<Maybe<TValue>> Max<TValue>(string propertyName) {
                throw new NotImplementedException();
            }

            public Task UpsertAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value, T document) {
                _dispatcher.Dispatch(MongoActionFactory.Default.Upsert<T>(_name, value, document));
                return Task.CompletedTask;
            }

            public async Task<bool> Exists<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value) {
                IAsyncEnumerable<T> items = await FindAll(propertySelector, value);
                List<T> list = await items.ToList();
                return list.Any();
            }
        }
    }
}

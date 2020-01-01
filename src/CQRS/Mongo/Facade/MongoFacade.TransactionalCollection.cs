using DX.Cqrs.Commons;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade
{
    partial class MongoFacade {
        class TransactionalCollectionFacade<T> : CollectionFacade<T>, ITransactionalMongoCollectionFacade<T> {
            private readonly IClientSessionHandle _session;

            public TransactionalCollectionFacade(IClientSessionHandle session, IMongoCollection<T> collection)
                : base(collection) => _session = session;

            public Task<IAsyncEnumerable<T>> FindAll<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value) {
                return FindAll(Builders<T>.Filter.Eq(propertySelector, value));
            }

            public async Task<bool> Exists<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value) {
                long count = await FindCore(Builders<T>.Filter.Eq(propertySelector, value))
                    .Limit(1)
                    .CountDocumentsAsync();

                return count > 0;
            }

            public async Task<IAsyncEnumerable<T>> FindAll(
                FilterDefinition<T>? filter = null,
                SortDefinition<T>? sort = null
            ) {
                filter = filter ?? Builders<T>.Filter.Empty;
                IFindFluent<T, T> find = FindCore(filter);

                if (sort != null)
                    find = find.Sort(sort);

                return new AsyncCursorEnumerable<T>(await find.ToCursorAsync());
            }

            public async Task<Maybe<TValue>> Max<TValue>(Expression<Func<T, TValue>> propertySelector) {
                var field = new ExpressionFieldDefinition<T, TValue>(propertySelector);

                // This seems to be a very fast way to get the max. See
                // https://docs.mongodb.com/manual/core/aggregation-pipeline-optimization/#agg-sort-limit-coalescence and
                // https://stackoverflow.com/questions/32076382/mongodb-how-to-get-max-value-from-collections/41052353
                //
                // We prefer ToListAsync here because an async curser might return empty Batches and code would get
                // a little more messy in this case.
                List<TValue> result = await Collection
                    .Find(_session, FilterDefinition<T>.Empty)
                    .Sort(Builders<T>.Sort.Descending(field))
                    .Limit(1)
                    .Project(propertySelector)
                    .ToListAsync();

                if (result.Any())
                    return result.Single();

                return None<TValue>.Value;
            }

            public async Task<Maybe<TValue>> Max<TValue>(string propertyName) {
                ValueHolder<TValue> result = await Collection
                    .Aggregate(_session)
                    .Sort(Builders<T>.Sort.Descending(propertyName))
                    .Limit(1)
                    .Project<ValueHolder<TValue>>($"{{ Value: '${propertyName}', _id: 0}}")
                    .FirstOrDefaultAsync();

                if (result == null)
                    return None<TValue>.Value;

                return result.Value;
            }

            protected virtual IFindFluent<T, T> FindCore(FilterDefinition<T> filter) {
                return Collection.Find(_session, filter);
            }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
            public class ValueHolder<TValue> {
                [BsonElement("Value")]
                public TValue Value { get; set; }
            }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. 

            public Task InsertManyAsync(IEnumerable<T> documents) {
                return Collection.InsertManyAsync(_session, documents);
            }

            public Task UpsertAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value, T document) {
                return Collection.FindOneAndReplaceAsync<T>(
                    _session,
                    Builders<T>.Filter.Eq(propertySelector, value),
                    document,
                    new FindOneAndReplaceOptions<T> { IsUpsert = true }
                );
            }
        }
    }
}

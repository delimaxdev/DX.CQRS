using DX.Cqrs.Commons;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DX.Cqrs.Mongo.Facade {
    public interface IMongoCollectionFacade<T> {
        Task CreateIndex(string elementName);
    }

    public interface ITransactionalMongoCollectionFacade<T> : IMongoCollectionFacade<T> {

        Task<IAsyncEnumerable<T>> FindAll(
            FilterDefinition<T>? filter = null,
            SortDefinition<T>? sort = null
        );

        Task<bool> Exists<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value);

        Task<IAsyncEnumerable<T>> FindAll<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value);

        Task<Maybe<TValue>> Max<TValue>(Expression<Func<T, TValue>> propertySelector);

        Task<Maybe<TValue>> Max<TValue>(string propertyName);

        Task InsertManyAsync(IEnumerable<T> documents);

        Task UpsertAsync<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value, T document);
    }
}

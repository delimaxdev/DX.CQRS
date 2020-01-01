using DX.Contracts;
using DX.Cqrs.Domain.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DX.Cqrs.Domain {
    public interface IRepository<T> where T : class, IPersistable {
        Task<bool> Exists(ID id);
        Task<T?> TryGet(ID id);
        Task Save(T @object);
    }

    public static class IRepositoryExtensions {
        public static async Task<T> Get<T>(this IRepository<T> repository, ID id) where T: class, IPersistable{
            return await repository.TryGet(id) ??
                throw new ArgumentException("An object with the given ID could not be found.", nameof(id));
        }

        public static Task<T> Get<T, TRef>(this IRepository<T> repository, Ref<TRef> reference) 
            where T : class, IPersistable, TRef
            where TRef : IHasID<ID> {

            return repository.Get(ID.FromRef(reference));
        } 

        public static IRepository<T> GetRepository<T>(this IServiceProvider services) where T : class, IPersistable {
            return services.GetRequiredService<IRepository<T>>();
        }
    }
}
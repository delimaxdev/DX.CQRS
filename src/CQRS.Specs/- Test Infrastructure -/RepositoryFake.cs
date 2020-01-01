using DX.Contracts;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DX.Testing {
    public class RepositoryFake<T> : IRepository<T> where T : class, IPersistable {
        private readonly Dictionary<ID, T> _objects = new Dictionary<ID, T>();

        public virtual Task<bool> Exists(ID id)
            => Task.FromResult(_objects.ContainsKey(id));

        public virtual Task Save(T @object) {
            if (@object.GetChanges().IsNew) {
                _objects.Add(@object.ID, @object);
            } else {
                _objects[@object.ID] = @object;
            }

            @object.ClearChanges();

            return Task.CompletedTask;
        }

        public Task<T> TryGet(ID id) {
            _objects.TryGetValue(id, out T result);            
            return Task.FromResult(result);
        }
    }
}
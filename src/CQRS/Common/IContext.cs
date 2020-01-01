using System;
using System.Collections.Generic;

namespace DX.Cqrs.Common {
    public interface IContext : IServiceProvider {
        bool TryGet(Type contextType, out object? instance);

        void Set(Type contextType, object instance, bool isPersistent);

        IContext Persist();

        void RestoreTo(IContext target);


        object IServiceProvider.GetService(Type serviceType) {
            if (TryGet(out IServiceProvider sp)) {
                return sp.GetService(serviceType);
            }

            throw new InvalidOperationException(
                "The given 'IContext' cannot be used to resolve services because it has " +
                "not set a context object of type 'IServiceProvider'.");
        }

        object Get(Type contextType) {
            if (TryGet(contextType, out object result))
                return result;

            throw new KeyNotFoundException(
                $"The given 'IContext' has no context object set of type '{contextType.Name}'."
            );
        }

        bool TryGet<T>(out T instance) {
            if (TryGet(typeof(T), out object untyped)) {
                instance = (T)untyped;
                return true;
            }

            instance = default!;
            return false;
        }

        T Get<T>()
            => (T)Get(typeof(T));

        void Set<T>(T instance, bool isPersistent)
            => Set(typeof(T), instance!, isPersistent);
    }
}
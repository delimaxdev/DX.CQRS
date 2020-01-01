using System;
using System.Collections;
using System.Collections.Generic;

namespace DX.Cqrs.Common {
    public class GenericContext : IContext {
        private readonly Dictionary<Type, ContextItem> _contexts = new Dictionary<Type, ContextItem>();

        public void Set(Type contextType, object instance, bool isPersistent) {
            Check.NotNull(contextType, nameof(contextType));
            Check.NotNull(instance, nameof(instance));

            _contexts[contextType] = new ContextItem {
                Instance = instance,
                IsPersistent = isPersistent
            };
        }
        
        public bool TryGet(Type contextType, out object? instance) {
            Check.NotNull(contextType, nameof(contextType));
            
            if (_contexts.TryGetValue(contextType, out ContextItem item)) {
                instance = item.Instance;
                return true;
            }

            instance = default;
            return false;
        }

        public IContext Persist() {
            IContext copy = new GenericContext();
            RestoreTo(copy);
            return copy;
        }

        public void RestoreTo(IContext target) {
            foreach (KeyValuePair<Type, ContextItem> pair in _contexts) {
                if (pair.Value.IsPersistent) {
                    target.Set(pair.Key, pair.Value.Instance, pair.Value.IsPersistent);
                }
            }
        }

        private struct ContextItem {
            public object Instance;
            public bool IsPersistent;
        }
    }
}
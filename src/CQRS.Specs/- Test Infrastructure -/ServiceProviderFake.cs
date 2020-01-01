using System;
using System.Collections.Generic;

namespace DX.Testing
{
    internal class ServiceProviderFake : IServiceProvider {
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        public ServiceProviderFake Register<T>(T service) where T : class
            => Register<T>(() => service);

        public ServiceProviderFake Register<T>(Func<T> serviceFactory) where T : class {
            _registrations.Add(typeof(T), serviceFactory);
            return this;
        }

        public object GetService(Type serviceType) {
            if (_registrations.TryGetValue(serviceType, out Func<object> valueFactory)) {
                return valueFactory();
            }

            return null;
        }
    }
}
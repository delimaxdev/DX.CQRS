using Microsoft.Extensions.DependencyInjection;
using System;

namespace DX {
    public static class ServiceProviderExtensions {
        public static T Resolve<T>(this IServiceProvider provider) {
            return (T)provider.Resolve(typeof(T));
        }

        public static object Resolve(this IServiceProvider provider, Type serviceType) {
            Check.NotNull(provider, nameof(provider));
            Check.NotNull(serviceType, nameof(serviceType));

            if (provider is ISupportRequiredService p) {
                return p.GetRequiredService(serviceType);
            }

            return provider.GetService(serviceType) ??
                throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
        }

        public static T GetService<T>(this IServiceProvider container) {
            return (T)container.GetService(typeof(T));
        }

        public static T GetService<T>(this IServiceProvider container, string serviceName) {
            return (T)container.GetService(typeof(T), serviceName);
        }

        public static object GetService(this IServiceProvider container, Type serviceType, string serviceName) {
            if (container is ContainerExtension ex) {
                return ex.GetService(serviceType, serviceName);
            } else {
                return container.GetAdapater().GetService(serviceType, serviceName);
            }
        }

        public static IServiceProvider With<TInterface>(this IServiceProvider container, TInterface implementation) {
            return container is ContainerExtension ex ?
                ex.AddDependency<TInterface>(implementation) :
                new ContainerExtension<TInterface>(container, implementation);
        }

        private static IContainerAdapter GetAdapater(this IServiceProvider container) {
            return container.GetService<IContainerAdapter>();
        }

        abstract class ContainerExtension : IServiceProvider {
            protected IServiceProvider Container { get; }

            public ContainerExtension(IServiceProvider container)
                => Container = container;

            public abstract ContainerExtension AddDependency<TInterface>(TInterface implementation);

            public object GetService(Type serviceType) {
                return GetService(serviceType, null);
            }

            public abstract object GetService(Type serviceType, string? serviceName);
        }

        class ContainerExtension<T1> : ContainerExtension {
            public T1 Arg1 { get; }

            public ContainerExtension(IServiceProvider container, T1 arg1)
                : base(container) => Arg1 = arg1;

            public override ContainerExtension AddDependency<TInterface>(TInterface implementation) {
                return new ContainerExtension<T1, TInterface>(Container, Arg1, implementation);
            }

            public override object GetService(Type serviceType, string? serviceName) {
                return Container.GetAdapater().GetService<T1>(serviceType, Arg1, serviceName);
            }
        }

        class ContainerExtension<T1, T2> : ContainerExtension<T1> {
            public T2 Arg2 { get; }

            public ContainerExtension(IServiceProvider container, T1 arg1, T2 arg2)
                : base(container, arg1) => Arg2 = arg2;

            public override ContainerExtension AddDependency<TInterface>(TInterface implementation) {
                throw new InvalidOperationException(
                    $"{nameof(ServiceProviderExtensions)} supports a maximum of two parameters"
                );
            }
            public override object GetService(Type serviceType, string? serviceName) {
                return Container.GetAdapater().GetService<T1, T2>(serviceType, Arg1, Arg2, serviceName);
            }
        }
    }

    public interface IContainerAdapter {
        object GetService(Type objectType, string serviceName);

        object GetService<T1>(Type serviceType, T1 arg1, string? serviceName);

        object GetService<T1, T2>(Type serviceType, T1 arg1, T2 arg2, string? serviceName);
    }
}

using Autofac;

namespace DX
{
    public static class AutofacExtensions {
        public static TService ResolveUnregistered<TService>(this IComponentContext context) {
            // Inspired by https://stackoverflow.com/questions/5043132/autofac-instantiating-an-unregistered-service-with-known-services
            var scope = context.Resolve<ILifetimeScope>();

            using (var innerScope = scope.BeginLifetimeScope(b => 
                b.RegisterType(typeof(TService)).ExternallyOwned())
            ) {
                return innerScope.Resolve<TService>();
            }
        }
    }
}

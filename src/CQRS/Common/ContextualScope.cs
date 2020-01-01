using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Cqrs.Common {
    public class ContextualScope : IDisposable, IServiceProvider {
        private readonly IServiceScope _scope;

        public IContext Context => this.GetRequiredService<IContext>();

        public ContextualScope(IServiceProvider services) {
            _scope = services.CreateScope();
        }

        public ContextualScope(IServiceProvider services, IContext context) : this(services) {
            context.RestoreTo(Context);
        }

        public object GetService(Type serviceType)
            => _scope.ServiceProvider.GetService(serviceType);

        public void Dispose()
            => _scope.Dispose();
    }
}
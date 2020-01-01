using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DX.Cqrs.Common {
    public class TransactionScope : ContextualScope, ITransaction {
        public TransactionScope(IServiceProvider services)
            : base(services) { }

        public TransactionScope(IServiceProvider services, IContext context) 
            : base(services, context) { }

        public Task CommitAsync()
            => this.GetRequiredService<ITransaction>().CommitAsync();

        public Task AbortAsync()
            => this.GetRequiredService<ITransaction>().AbortAsync();
    }
}
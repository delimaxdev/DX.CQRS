using DX.Cqrs.Common;
using System;

namespace DX.Web
{
    public class DXContext : GenericContext {
        public DXContext(IServiceProvider services) {
            Set(typeof(IServiceProvider), Check.NotNull(services, nameof(services)), isPersistent: false);
        }
    }
}
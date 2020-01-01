using DX.Contracts.ReadModels;
using DX.Cqrs.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public static class QueryExtensions {
        public static BindingFlags Bindingflags { get; private set; }

        public static Task<TResult> RunQuery<TResult>(this IServiceProvider services, ICriteria<TResult> query) {
            Type queryType = typeof(IQuery<,>)
                .MakeGenericType(query.GetType(), typeof(TResult));

            object impl = services.GetRequiredService(queryType);
            IContext ctx = services.GetRequiredService<IContext>();
           
            return (Task<TResult>)queryType
                .GetMethod("Run")
                .Invoke(impl, new object[] { query, ctx });
        }

        public static Task<TResult> RunQuery<TResult>(this IContext context, ICriteria<TResult> query)
            => context.Get<IServiceProvider>().RunQuery(query);
    }
}
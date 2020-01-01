using DX.Contracts.ReadModels;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DX.Web.Controllers
{
    public class QueriesControllerBase : ControllerBase {
        private static readonly MethodInfo __runQueryMethod = typeof(QueriesControllerBase)
            .GetMethod(nameof(RunQuery), BindingFlags.NonPublic | BindingFlags.Instance);

        [HttpPost]
        public object Execute([FromBody] ICriteria query) {
            Check.NotNull(query, nameof(query));

            Type criteriaType = query.GetType();
            Type resultType = ReflectionUtils
                .GetGenericInterfaceImplementations(criteriaType, typeof(ICriteria<>))
                .Single()
                .GenericTypeArguments
                .Single();

            return ((Task<object>)__runQueryMethod
                .MakeGenericMethod(criteriaType, resultType)
                .Invoke(this, new[] { query })).Result;
        }

        private async Task<object> RunQuery<TCriteria, TResult>(TCriteria query) 
            where TCriteria : ICriteria<TResult> {

            var executor = HttpContext.RequestServices.GetRequiredService<IQuery<TCriteria, TResult>>();
            return await executor.Run(query, HttpContext.RequestServices.GetRequiredService<IContext>());
        }
    }
}
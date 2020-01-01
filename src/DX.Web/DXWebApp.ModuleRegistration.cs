using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DX.Web {
    partial class DXWebApp {
        // This approach was inspired by https://www.strathweb.com/2017/04/running-multiple-independent-asp-net-core-pipelines-side-by-side-in-the-same-application/
        // and https://github.com/WebApiContrib/WebAPIContrib.Core/blob/master/src/WebApiContrib.Core/ParallelApplicationPipelinesExtensions.cs
        // although heavily modified. 
        private class ModuleRegistration {
            private readonly PathString _path;
            private IWebHost _moduleHost;

            public DXModule Instance { get; }

            private string ModuleName => Instance.GetType().ToString();

            public ModuleRegistration(DXModule instance, PathString path) {
                Instance = Check.NotNull(instance, nameof(instance));
                _path = Check.NotNull(path, nameof(path));
            }

            public void ConfigureServices(DXWebApp app) {
                // We use the WebHostBuilder mainly to create a child container with all the 
                // ASP.NET Core registrations
                _moduleHost = new WebHostBuilder()
                    .ConfigureServices(services => {
                        // We use a blank ApplicationPartManager to avoid autodiscovery. Otherwise ASP.NET would for
                        // some reason discover and add all the Controllers from ALL assemblies.
                        services.AddSingleton(new ApplicationPartManager());

                        Instance.ConfigureServices(services, app.GlobalContainer.Resolve<IServiceProvider>());

                        // We create a child container that is specific per module but we inherit the global container.
                        ILifetimeScope moduleContainer = app.GlobalContainer.BeginLifetimeScope(
                            ModuleName,
                            c => {
                                c.Populate(services, ModuleName);
                                Instance.ConfigureContainer(c);
                                app.OnConfigureModuleServices(Instance.GetType(), c);
                            });

                        services.AddSingleton<IStartup>(new AutofacStartup(moduleContainer));
                    })
                    .Build();
            }

            public void Configure(IApplicationBuilder globalBuilder) {
                IServiceProvider modulesServices = _moduleHost.Services;

                // This is basically some code, which normally gets executed by WebHost.BuildApplication
                // (https://github.com/aspnet/AspNetCore/blob/master/src/Hosting/Hosting/src/Internal/WebHost.cs).
                IApplicationBuilder branchBuilder = modulesServices
                    .GetRequiredService<IApplicationBuilderFactory>()
                    .CreateBuilder(_moduleHost.ServerFeatures);

                // For each request, we start a new child scope. This means that all all services registered
                // "Per Lifetime Scope" will be specific per request and will all be disposed at the end of
                // the request.
                IServiceScopeFactory scopeFactory = modulesServices.GetRequiredService<IServiceScopeFactory>();

                branchBuilder.Use(async (context, next) => {
                    IServiceProvider original = context.RequestServices;

                    using (IServiceScope scope = scopeFactory.CreateScope()) {
                        context.RequestServices = scope.ServiceProvider;

                        var httpContextAccessor = context
                            .RequestServices
                            .GetService<IHttpContextAccessor>();

                        if (httpContextAccessor != null)
                            httpContextAccessor.HttpContext = context;

                        await next();
                    }

                    context.RequestServices = original;
                });

                Instance.Configure(branchBuilder);

                RequestDelegate branchDelegate = branchBuilder.Build();
                globalBuilder.Map(_path, b =>
                    b.Use(async (context, next) => await branchDelegate(context)));
            }

            private class AutofacStartup : IStartup {
                private readonly ILifetimeScope _scope;

                public AutofacStartup(ILifetimeScope scope) => _scope = scope;

                public void Configure(IApplicationBuilder app) { }

                public IServiceProvider ConfigureServices(IServiceCollection services)
                    => new AutofacServiceProvider(_scope);
            }
        }
    }
}

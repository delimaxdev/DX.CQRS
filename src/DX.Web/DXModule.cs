using Autofac;
using DX.Cqrs.Application;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Cqrs.Domain;
using DX.Cqrs.EventStore;
using DX.Cqrs.Maintenance;
using DX.Cqrs.Mongo.Facade;
using DX.Web.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DX.Web
{
    public class DXModule : Module {
        public IServiceProvider Services { get; private set; }

        public DXModule(IConfiguration configuration)
            => Configuration = Check.NotNull(configuration, nameof(configuration));

        protected internal IConfiguration Configuration { get; set; }

        protected internal virtual void ConfigureGlobalContainer(ContainerBuilder globalContainer) { }

        protected internal virtual void ConfigureServices(IServiceCollection services, IServiceProvider globalServices) { }

        protected internal virtual void ConfigureContainer(ContainerBuilder builder) {
            builder.RegisterGeneric(typeof(OptionsInitializer<>)).AsImplementedInterfaces().SingleInstance();

            // HACK: Is there a better way to make this really async?? Maybe refactor ITransaction...
            builder.Register<ITransaction>(context => context
                    .Resolve<IMongoFacade>()
                    .StartTransactionAsync()
                    .Result)
                .InstancePerLifetimeScope();

            builder.Register<IEventStoreTransaction>(context =>
                context.Resolve<IEventStore>().UseTransaction(
                    context.Resolve<ITransaction>()))
                .InstancePerLifetimeScope();

            builder.RegisterType<DXContext>()
                .As<IContext>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DefaultCommandMetadataProvider>()
                .As<ICommandMetadataProvider>()
                .SingleInstance();

            builder.RegisterType<DefaultEventMetadataProvider>()
                .As<IEventMetadataProvider>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MaintenanceService>()
                .As<ICommandHandler>()
                .As<IMaintenanceService>()
                .SingleInstance();

            builder.RegisterType<CommandProcessor>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<ScriptHandler>()
                .AsImplementedInterfaces()
                .SingleInstance();
        }

        protected internal virtual void Configure(IApplicationBuilder app) {
            Services = app.ApplicationServices;

            app.Use((context, next) => {
                // TODO: Populate IContext here...
                return next();
            });

            Migrate(app.ApplicationServices.GetRequiredService<IMaintenanceService>());
        }

        protected internal virtual void Migrate(IMaintenanceService service) { }
    }
}

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.Metadata;
using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Contracts.Serialization;
using DX.Cqrs.EventStore;
using DX.Cqrs.EventStore.Mongo;
using DX.Cqrs.Mongo.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DX.Web {
    public class ConfigureModuleServicesEventArgs {
        public Type ModuleType { get; }

        public ContainerBuilder Container { get; }

        public ConfigureModuleServicesEventArgs(Type moduleType, ContainerBuilder container) {
            ModuleType = moduleType;
            Container = container;
        }
    }

    public abstract partial class DXWebApp : IStartup {
        private const string PathMetadataKey = "Path";
        private ModuleRegistration[] _modules;

        public ILifetimeScope GlobalContainer { get; private set; }

        public event EventHandler<ConfigureModuleServicesEventArgs> ConfigureModuleServices;
        
        protected IConfiguration Configuration { get; }
        private IContainer BootstrapContainer { get; set; }

        public DXWebApp(IConfiguration configuration)
            => Configuration = Check.NotNull(configuration, nameof(configuration));

        private ModuleRegistration[] CreateModules() {
            return BootstrapContainer.Resolve<IEnumerable<Meta<DXModule>>>()
                .Select(x => new ModuleRegistration(x.Value, (PathString)x.Metadata[PathMetadataKey]))
                .ToArray();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services) {
            ContainerBuilder bootstrapBuilder = new ContainerBuilder();
            ConfigureBootstrapContainer(bootstrapBuilder);
            ModuleRegistrator registrator = new ModuleRegistrator(bootstrapBuilder);
            RegisterModules(registrator);
            BootstrapContainer = bootstrapBuilder.Build();

            ConfigureGlobalServices(services);
            IServiceProvider aspNetServices = default;

            _modules = CreateModules();
            GlobalContainer = BootstrapContainer.BeginLifetimeScope(globalBuilder => {
                aspNetServices = services.BuildServiceProvider();

                // We do not use the root Container directly for the root Startup, because all the ASP.NET
                // registrations of the root Startup would be inherited to the module branches (including
                // things like IStartup registration which is a very bad idea to inherit to child modules).
                // Therefore we only add selected services to the global container (done in 
                // ConfigureGlobalContainer).
                ConfigureGlobalContainer(globalBuilder, aspNetServices);
                _modules.ForEach(m => m.Instance.ConfigureGlobalContainer(globalBuilder));
            });
            _modules.ForEach(m => m.ConfigureServices(this));

            return aspNetServices;
        }

        public void Configure(IApplicationBuilder app) {
            ConfigureGlobal(app);
            _modules.ForEach(m => m.Configure(app));
            _modules = null;
        }

        protected abstract void RegisterModules(ModuleRegistrator modules);

        protected virtual void ConfigureBootstrapContainer(ContainerBuilder bootstrapBuilder) {
            bootstrapBuilder.RegisterInstance(Configuration);
        }

        protected virtual void ConfigureGlobalServices(IServiceCollection services) { }

        protected virtual void ConfigureGlobalContainer(ContainerBuilder builder, IServiceProvider aspnetServices) {
            builder.RegisterGeneric(typeof(OptionsFactory<>)).As(typeof(IOptionsFactory<>)).SingleInstance();
            builder.RegisterGeneric(typeof(OptionsManager<>)).As(typeof(IOptions<>)).SingleInstance();
            builder.RegisterGeneric(typeof(OptionsMonitor<>)).As(typeof(IOptionsMonitor<>)).SingleInstance();


            builder.RegisterType<AutofacServiceProvider>().As<IServiceProvider>();
            builder.RegisterInstance(aspnetServices.GetService<IServer>());

            builder.RegisterType<DefaultSerializerSetup>().As<IConfigureSerializers>().SingleInstance();
            builder.RegisterType<SerializationTypeRegistry>().SingleInstance();
            builder.RegisterType<SerializerManager>().SingleInstance();
        }

        protected virtual void ConfigureGlobal(IApplicationBuilder app) {
            ConfigureEventStore();
            app.UseHttpsRedirection();
        }

        protected void ConfigureEventStore() {
            ContractTypeSerializer contractTypeSerializer = GlobalContainer
                .Resolve<SerializerManager>()
                .ContractTypeSerializer; ;

            SerializationTypeRegistry serializationTypes = GlobalContainer
                .Resolve<SerializationTypeRegistry>();

            MongoEventStoreSerializatonSettings settings = new MongoEventStoreSerializatonSettings(
                new IDSerializer(),
                new ContractTypeSerializerAdapter<object, IEvent>(contractTypeSerializer),
                new ContractTypeSerializerAdapter<object, DefaultEventMetadata>(contractTypeSerializer),
                new TypeNameResolver(serializationTypes)
            );

            MongoEventStore.ConfigureSerialization(settings);
        }

        protected class ModuleRegistrator {
            private readonly ContainerBuilder _bootstrapBuilder;

            internal ModuleRegistrator(ContainerBuilder bootstrapBuilder) =>
                _bootstrapBuilder = bootstrapBuilder;

            public void Register<TModule>(PathString path) where TModule : DXModule =>
                _bootstrapBuilder.RegisterType<TModule>()
                    .As<DXModule>()
                    .As<TModule>()
                    .SingleInstance()
                    .WithMetadata(PathMetadataKey, path);
        }

        internal void OnConfigureModuleServices(Type moduleType, ContainerBuilder moduleContainer) {
            ConfigureModuleServices?.Invoke(this, new ConfigureModuleServicesEventArgs(moduleType, moduleContainer));
        }
    }
}
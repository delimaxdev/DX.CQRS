using Autofac;
using Autofac.Extensions.DependencyInjection;
using DX.Contracts;
using DX.Contracts.Serialization;
using DX.Cqrs.Application;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using DX.Cqrs.EventStore;
using DX.Cqrs.Maintenance;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Testing {
    public class TestServices : IServiceProvider, IDisposable {
        private readonly IContainer _container;
        private readonly IServiceProvider _serviceProvider;

        public TestServices(Action<TestServicesBuilder> config) {
            ContainerBuilder builder = new ContainerBuilder();
            config(new TestServicesBuilder(builder));

            _container = builder.Build();
            _serviceProvider = _container.Resolve<IServiceProvider>();
        }

        public TestServices(IServiceProvider services) {
            _container = null;
            _serviceProvider = services;
        }

        public TestServices() : this(b => b.UseDefaults()) { }

        public Task Run(MaintenanceScript script) {
            return _serviceProvider
                .Resolve<IMaintenanceService>()
                .RunScript(script);
        }

        public void Execute(ICommandMessage command, ID? targetID = null)
            => Execute(new Command(command, targetID));

        public void Execute(IBuilds<ICommandMessage> command, ID? targetID = null)
            => Execute(new Command(command.Build(), targetID));

        public void Execute<TRef>(ICommandMessage command, Ref<TRef> target) where TRef : IHasID<ID>
            => Execute(new Command(command, ID.FromRef(Check.NotNull(target, nameof(target)))));

        public void Execute<TRef>(IBuilds<ICommandMessage> message, Ref<TRef> target) where TRef : IHasID<ID>
            => Execute(new Command(message.Build(), ID.FromRef(Check.NotNull(target, nameof(target)))));

        public void Execute(params ICommandMessage[] messages) {
            Execute(messages.Select(m => new Command(m)).ToArray());
        }

        public void Execute(params Command[] commands) {
            IContext context = new GenericContext();
            context.Set(new RequestTimeContext(DateTime.Now), true);
            foreach (Command command in commands) {
                this.Send<QueueCommand, Task<QueueCommandResult>>(new QueueCommand(command, context))
                    .To<ICommandProcessor>()
                    .Result
                    .Completion
                    .Wait();
            }
        }

        public void Save<T>(T @object) where T : class, IPersistable {
            using (TransactionScope tx = new TransactionScope(this)) {
                tx.Context.Set(new TimestampContext(DateTime.Now), true);
                tx.Context.Set(new CommandContext(ID.NewID()), true); // HACK
                tx.GetRequiredService<IRepository<T>>().Save(@object).Wait();
                tx.CommitAsync().Wait();
            }
        }

        public object GetService(Type serviceType) {
            return _serviceProvider.GetService(serviceType);
        }

        public void Dispose() {
            _container?.Dispose();
        }
    }

    public class TestServicesBuilder {
        private readonly ContainerBuilder _container;

        public TestServicesBuilder(ContainerBuilder container) {
            _container = container;
            Services(s => s.Populate(Enumerable.Empty<ServiceDescriptor>()));
        }

        public TestServicesBuilder UseDefaults() {
            UseTransactionFake();
            UseEventStoreFake();
            UseRepositories();
            UseCommands();
            UseSerialization();
            UseContexts();
            UseMaintenanceService();
            return this;
        }

        public TestServicesBuilder UseContexts() => Services(s => {
            s.RegisterType<DXContext>().As<IContext>().InstancePerLifetimeScope();
        });

        public TestServicesBuilder UseCommands() => Services(s => {
            s.RegisterType<CommandProcessor>().As<ICommandProcessor>().SingleInstance();
            s.RegisterType<DefaultCommandMetadataProvider>().As<ICommandMetadataProvider>();
            s.Register(c => new TaskQueue(4)).As<ITaskQueue>().SingleInstance();
            s.RegisterType<ScriptHandler>().AsImplementedInterfaces().SingleInstance();

        });

        public TestServicesBuilder UseSerialization() => Services(s => {
            s.RegisterType<DefaultSerializerSetup>().As<IConfigureSerializers>().SingleInstance();
            s.RegisterType<SerializationTypeRegistry>().SingleInstance();
            s.RegisterType<SerializerManager>().SingleInstance();
        });

        public TestServicesBuilder UseTransactionFake() => Services(s => {
            s.RegisterType<TransactionMock>().As<ITransaction>().InstancePerLifetimeScope();
        });

        public TestServicesBuilder UseEventStoreFake() => Services(s => {
            s.RegisterType<TestEventStore>().As<IEventStore>().SingleInstance();
            s.Register<IEventStoreTransaction>(context =>
                    context.Resolve<IEventStore>().UseTransaction(
                        context.Resolve<ITransaction>()))
                .InstancePerLifetimeScope();

        });

        public TestServicesBuilder UseMaintenanceService() => Services(s => {
            s.RegisterType<MaintenanceService>()
                .As<ICommandHandler>()
                .As<IMaintenanceService>()
                .SingleInstance();
        });

        public TestServicesBuilder UseRepositories() => Services(s => {
            s.RegisterType<DefaultEventMetadataProvider>().As<IEventMetadataProvider>().InstancePerLifetimeScope();
            s.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
        });

        public TestServicesBuilder HandleAllCommands()
            => throw new NotImplementedException();

        public TestServicesBuilder Handle<TMessage>(Action<TMessage> action = null) where TMessage : IMessage
            => Services(s => s.RegisterInstance(
                new DelegateHandler<TMessage>(action ?? delegate { })).AsImplementedInterfaces());

        public TestServicesBuilder Services(Action<ContainerBuilder> config) {
            config(_container);
            return this;
        }

        private class DXContext : GenericContext {
            public DXContext(IServiceProvider services) {
                Set(typeof(IServiceProvider), services, isPersistent: false);
                Set(typeof(RequestTimeContext), new RequestTimeContext(DateTime.Now), true);
            }
        }

        private class DelegateHandler<TMessage> : Receivable, ICommandHandler {
            private readonly Action<TMessage> _action;

            public DelegateHandler(Action<TMessage> action) {
                _action = action;
                Register<HandleCommand, Task>(Handle);
            }

            private Maybe<Task> Handle(HandleCommand hc) {
                if (hc.Command.Message is TMessage m) {
                    _action(m);
                    return Task.CompletedTask;
                }

                return None<Task>.Value;
            }
        }
        private class TransactionMock : ITransaction {
            public Task AbortAsync()
                => Task.CompletedTask;

            public Task CommitAsync()
                => Task.CompletedTask;

            public void Dispose() { }
        }
    }
}
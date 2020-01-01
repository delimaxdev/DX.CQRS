using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Contracts.Serialization;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;
using static DX.Cqrs.Commands.ICommandProcessor;
using static DX.Cqrs.Commands.ITaskQueue;

namespace DX.Cqrs.Commands {
    public class CommandProcessor : Receivable, ICommandProcessor {
        public CommandProcessor(
            IServiceProvider services,
            ICommandMetadataProvider metadataProvider,
            SerializerManager manager
        ) {
            AddHandler(new QueueOperationDebouncer());
            AddHandler(new TransactionScopeHandler(services));
            AddHandler(new CommandPersister(services, metadataProvider, manager.ContractTypeSerializer));
            AddHandler(new Executor(this, services));
        }

        private class QueueOperationDebouncer : MessageHandler {
            private readonly ConcurrentDictionary<ID, Nothing> _currentCommands =
                new ConcurrentDictionary<ID, Nothing>();

            public QueueOperationDebouncer() {
                Register((QueueCommand q) => {
                    return _currentCommands.TryAdd(q.Command.ID, Nothing.Value) ?
                        Next(q) :
                        Task.FromResult(QueueCommandResult.AlreadyProcessing());
                });

                Register(async (ExecuteCommand e) => {
                    try {
                        await Next(e).Get();
                    } finally {
                        _currentCommands.TryRemove(e.Command.ID, out _);
                    }
                });
            }
        }

        private class CommandPersister : MessageHandler {
            public CommandPersister(
                IServiceProvider services,
                ICommandMetadataProvider metadataProvider,
                ContractTypeSerializer serializer
            ) {
                Register(async (QueueCommand q) => {
                    var queueSource = new TaskCompletionSource<QueueCommandResultType>();
                    var ctx = new CommandPersisterContext(queueSource.Task);
                    var resultType = QueueCommandResultType.Unknown;

                    try {
                        q.Context.Set(ctx, true);
                        QueueCommandResult result = await Next(q).Get();
                        resultType = result.Type;

                        if (result.Type == QueueCommandResultType.SuccessfullyQueued) {
                            using (var scope = new TransactionScope(services, q.Context)) {
                                scope.Context.Set(new TimestampContext(DateTime.Now), true);
                                scope.Context.Set(new CommandContext(q.Command.ID), true);

                                QueueHelper h = new QueueHelper(
                                    serializer,
                                    repository: scope.GetRepository<ServerCommand>(),
                                    command: q.Command,
                                    metadata: metadataProvider.Provide(q.Context)
                                );

                                if (await h.LoadExisting()) {
                                    if (!h.ExistingMatchesCurrent()) {
                                        resultType = QueueCommandResultType.Rejected;
                                        return QueueCommandResult.Rejected();
                                    }

                                    if (h.ExistingCompletedSuccessfully()) {
                                        resultType = QueueCommandResultType.AlreadyExecuted;
                                        return QueueCommandResult.AlreadyExecuted();
                                    }
                                }

                                ctx.Command = await h.CompleteQueue();
                                await scope.CommitAsync();
                            }
                        }

                        return result;
                    } finally {
                        queueSource.SetResult(resultType);
                    }
                });

                Register(async (ExecuteCommand e) => {
                    var persisterCtx = e.Context.Get<CommandPersisterContext>();

                    if (await persisterCtx.QueueTask != QueueCommandResultType.SuccessfullyQueued) {
                        return;
                    }

                    ServerCommand sc = persisterCtx.Command.NotNull();
                    IServiceProvider sp = e.Context.Get<IServiceProvider>();

                    sc.BeginExecution(DateTime.Now);
                    await sp
                        .GetRepository<ServerCommand>()
                        .Save(sc);
                    await sp.GetRequiredService<ITransaction>().CommitAsync();

                    try {
                        await Next(e).Get();

                        // We have to use a new transaction here, because the oiginal one may time
                        // out if the execution of the command takes very long.
                        using (var scope = new TransactionScope(sp, e.Context)) {
                            sc.EndExecution(DateTime.Now);
                            await scope
                                .GetRepository<ServerCommand>()
                                .Save(sc);
                            await scope.CommitAsync();
                        }
                    } catch (Exception ex) when (!ex.IsCritical()) {
                        using (var scope = new TransactionScope(services, e.Context)) {
                            scope.Context.Set(new TimestampContext(DateTime.Now), true);
                            scope.Context.Set(new CommandContext(e.Command.ID), true);

                            sc.EndExecution(DateTime.Now, ex);
                            await scope
                                .GetRepository<ServerCommand>()
                                .Save(sc);

                            await scope.CommitAsync();
                        }
                        
                        throw;
                    }
                });
            }
            private class QueueHelper {
                private readonly ContractTypeSerializer _serializer;
                private readonly IRepository<ServerCommand> _repository;
                private readonly Command _command;
                private readonly CommandMetadata _metadata;
                private ServerCommand? _existing;

                public QueueHelper(
                    ContractTypeSerializer serializer,
                    IRepository<ServerCommand> repository,
                    Command command,
                    CommandMetadata metadata
                ) {
                    _serializer = serializer;
                    _repository = repository;
                    _command = command;
                    _metadata = metadata;
                }

                public async Task<bool> LoadExisting() {
                    _existing = await _repository.TryGet(_command.ID);
                    return _existing != null;
                }

                public bool ExistingMatchesCurrent() =>
                    Object.Equals(_command.Target, _existing!.Target) &&
                    // JToken does not override Equals!
                    JObject.DeepEquals(Serialize(_command.Message), Serialize(_existing!.Message));

                public bool ExistingCompletedSuccessfully()
                    => _existing!.IsCompletedSuccessfully;

                public async Task<ServerCommand> CompleteQueue() {
                    ServerCommand sc = _existing ?? new ServerCommand(_command.ID, _command.Target, _command.Message);
                    sc.Queued(DateTime.Now, _metadata);
                    await _repository.Save(sc);
                    return sc;
                }

                private JToken Serialize(ICommandMessage message) {
                    using JTokenWriter writer = new JTokenWriter();
                    _serializer.Serialize(writer, message);
                    return writer.Token;
                }
            }

            private class CommandPersisterContext {
                public ServerCommand? Command { get; set; }
                public Task<QueueCommandResultType> QueueTask { get; }
                public CommandPersisterContext(Task<QueueCommandResultType> queueTask)
                    => QueueTask = queueTask;
            }
        }

        private class TransactionScopeHandler : MessageHandler {
            public TransactionScopeHandler(IServiceProvider services) {
                Register(async (ExecuteCommand e) => {
                    using (var scope = new TransactionScope(services, e.Context)) {
                        scope.Context.Set(new CommandContext(e.Command.ID), true);
                        scope.Context.Set(new TimestampContext(DateTime.Now), true);

                        e.Context = scope.Context;

                        await Next(e).Get();
                        try {
                            await scope.CommitAsync();
                        } catch (Exception ex) {

                            // HACK!!!!!!!!!!!!!!!!!!!!!!!!!
                        }
                    }
                });
            }
        }

        private class Executor : MessageHandler {
            private readonly CommandProcessor _processor;

            private IServiceProvider Services { get; }

            public Executor(CommandProcessor processor, IServiceProvider services) {
                _processor = Check.NotNull(processor, nameof(processor));
                Services = Check.NotNull(services, nameof(services));
                Register<QueueCommand, Task<QueueCommandResult>>(Handle);
                Register<ExecuteCommand, Task>(Handle);
            }

            private async Task<QueueCommandResult> Handle(QueueCommand m) {
                TaskQueueItem item = new ExecuteCommandTaskQueueItem(_processor, m.Command, m.Context.Persist());

                await Services
                    .Send<Enqueue, Task>(new Enqueue(item))
                    .To<ITaskQueue>();

                return QueueCommandResult.SuccessfullyQueued(item.Completion);
            }

            private async Task Handle(ExecuteCommand m) {
                HandleCommand h = new HandleCommand(m.Command, m.Context);
                await Services.Send<HandleCommand, Task>(h).To<ICommandHandler>();
            }

            private class ExecuteCommandTaskQueueItem : TaskQueueItem {
                private readonly Command _command;
                private readonly CommandProcessor _processor;
                private readonly IContext _context;
                private readonly object _target;

                public ExecuteCommandTaskQueueItem(
                    CommandProcessor processor,
                    Command command,
                    IContext executeContext
                ) {
                    _processor = processor;
                    _command = command;
                    _context = executeContext;
                    _target = command.Target ?? new Object();
                }

                public override object TargetID => _target;

                protected override Task ProcessCore() {
                    return _processor.Send<ExecuteCommand, Task>(new ExecuteCommand(_command, _context));
                }
            }
        }
    }
}

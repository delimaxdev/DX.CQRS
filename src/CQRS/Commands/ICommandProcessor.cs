using DX.Contracts;
using DX.Cqrs.Common;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Cqrs.Commands {
    public interface ICommandProcessor :
        IReceives<QueueCommand>,
        IReceives<ExecuteCommand> {

        public class QueueCommand : IMessage<Task<QueueCommandResult>> {
            public QueueCommand(Command command, IContext context) {
                Command = Check.NotNull(command, nameof(command));
                Context = Check.NotNull(context, nameof(context));
            }

            public Command Command { get; }

            public IContext Context { get; }
        }

        public class ExecuteCommand : IMessage<Task> {
            public Command Command { get; }

            public IContext Context { get; set; }

            public ExecuteCommand(Command command, IContext context) {
                Command = Check.NotNull(command, nameof(command));
                Context = Check.NotNull(context, nameof(context));
            }
        }

        public class QueueCommandResult {
            public QueueCommandResultType Type { get; }

            public Task Completion { get; }

            private QueueCommandResult(QueueCommandResultType type, Task completion) {
                Type = type;
                Completion = completion;
            }

            public static QueueCommandResult Rejected()
                => new QueueCommandResult(QueueCommandResultType.Rejected, Task.CompletedTask);

            public static QueueCommandResult AlreadyExecuted()
                => new QueueCommandResult(QueueCommandResultType.AlreadyExecuted, Task.CompletedTask);

            public static QueueCommandResult AlreadyProcessing()
                => new QueueCommandResult(QueueCommandResultType.AlreadyProcessing, Task.CompletedTask);
            
            public static QueueCommandResult SuccessfullyQueued(Task completion)
                => new QueueCommandResult(QueueCommandResultType.SuccessfullyQueued, completion);
        }

        public enum QueueCommandResultType {
            Unknown,
            Rejected,
            AlreadyProcessing,
            AlreadyExecuted,
            SuccessfullyQueued
        }
    }

    public static class ICommandProcessorExtensions {
        public static Task<QueueCommandResult> Enqueue(this IServiceProvider services, ID commandID, ICommandMessage message) {
            Command c = new Command(commandID, message);

            IContext context = services.GetRequiredService<IContext>();
            QueueCommand qc = new QueueCommand(c, context);

            return services.Send<QueueCommand, Task<QueueCommandResult>>(qc).To<ICommandProcessor>();
        }


    }
}
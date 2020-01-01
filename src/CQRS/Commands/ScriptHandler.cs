using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Cqrs.Commands {
    public class ScriptHandler : Receivable, ICommandHandler {
        private readonly IServiceProvider _services;

        public ScriptHandler(IServiceProvider services) {
            _services = Check.NotNull(services, nameof(services));
            Register<HandleCommand, Task>(Handle);
        }

        private Maybe<Task> Handle(HandleCommand hc) {
            if (hc.Command.Message is RunScript script) {
                return Handle(script, hc.Context);
            }

            return None<Task>.Value;
        }

        private async Task Handle(RunScript script, IContext context) {
            foreach (ScriptCommand scriptCommand in script.Script.Commands) {
                using (TransactionScope scope = new TransactionScope(_services, context)) {
                    scope
                        .Context
                        .Set(new RequestTimeContext(DateTime.Now), true);

                    Command command = new Command(
                        scriptCommand.ID,
                        scriptCommand.Message,
                        scriptCommand.Target);

                    QueueCommandResult result = await _services
                        .Send<QueueCommand, Task<QueueCommandResult>>(new QueueCommand(command, scope.Context))
                        .To<ICommandProcessor>();

                    await result.Completion;
                }
            }
        }
    }
}
using DX.Contracts;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using static DX.Contracts.Cqrs.Domain.IMaintenanceCommands;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Cqrs.Maintenance {
    public class MaintenanceService : CommandHandler, IMaintenanceService {
        private readonly IServiceProvider _services;

        public MaintenanceService(IServiceProvider services) {
            _services = Check.NotNull(services, nameof(services));
            HandleCommand<RunMaintenanceScript>(Handle);
        }

        public async Task RunScript(MaintenanceScript script) {
            ScriptCommand[] commands = script
                .Build()
                .ToArray();

            var message = new RunMaintenanceScript(
                new Script(script.Name, commands));

            using (var scope = new ContextualScope(_services)) {
                scope.Context.Set(new RequestTimeContext(DateTime.Now), true);
                var result = scope
                    .Enqueue(script.ScriptID, message)
                    .Result;

                if (result.Type == QueueCommandResultType.Rejected)
                    throw new InvalidOperationException("TODO: Better handling...");

                await result.Completion;
            }
        }
        private async Task Handle(RunMaintenanceScript r, IContext context) {
            using (var scope = new ContextualScope(_services, context)) {
                scope.Context.Set(new RequestTimeContext(DateTime.Now), true);
                QueueCommandResult result = await scope.Enqueue(ID.NewID(), new RunScript(r.Script));

                if (result.Type == QueueCommandResultType.Rejected)
                    throw new InvalidOperationException("TODO: Better handling...");

                await result.Completion;
            }
        }
    }
}
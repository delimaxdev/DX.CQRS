using DX.Cqrs.Commands;

namespace DX.Contracts.Cqrs.Domain {
    public interface IMaintenanceCommands {
        [Contract]
        public class RunMaintenanceScript : ICommandMessage {
            public Script Script { get; }

            public RunMaintenanceScript(Script script)
                => Script = Check.NotNull(script, nameof(script));
        }
    }
}
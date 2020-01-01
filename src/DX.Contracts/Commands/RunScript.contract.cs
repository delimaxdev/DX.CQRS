using DX.Contracts;

namespace DX.Cqrs.Commands {
    [Contract]
    public partial class RunScript : ICommandMessage {
        public Script Script { get; }

        public RunScript(Script script)
            => Script = Check.NotNull(script, nameof(script));
    }
}
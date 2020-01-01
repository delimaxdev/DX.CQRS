using DX.Contracts;

namespace DX.Cqrs.Commands
{
    public class CommandContext {
        public ID ExecutingCommand { get; }

        public CommandContext(ID executingCommand) {
            ExecutingCommand = executingCommand;
        }
    }
}
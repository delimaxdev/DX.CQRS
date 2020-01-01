using DX.Cqrs.Common;
using DX.Messaging;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;

namespace DX.Cqrs.Commands {
    public interface ICommandHandler : IReceives<HandleCommand> {
        public class HandleCommand : IMessage<Task> {
            public Command Command { get; }

            public IContext Context { get; }

            public HandleCommand(Command command, IContext context) {
                Command = Check.NotNull(command, nameof(command));
                Context = Check.NotNull(context, nameof(context));
            }
        }
    }
}

using DX.Contracts;

namespace DX.Cqrs.Commands {
    [Builder(typeof(CommandBuilder))]
    [Contract]
    public class Command {
        public ID ID { get; }

        public ID? Target { get; }

        public ICommandMessage Message { get; }

        public Command(ICommandMessage message, ID? targetID = null)
            : this(ID.NewID(), message, targetID) { }
        
        public Command(ID commandID, ICommandMessage message, ID? targetID = null) {
            ID = Check.NotNull(commandID, nameof(commandID));
            Target = targetID;
            Message = Check.NotNull(message, nameof(message));
        }
    }

    public class CommandBuilder : IBuilds<Command> {
        public ID? ID { get; set; }

        public ID? Target { get; set; }

        public ICommandMessage? Message { get; set; }

        public CommandBuilder(Command? source = null) {
            if (source != null) {
                ID = source.ID;
                Target = source.Target;
                Message = source.Message;
            }
        }

        public Command Build() {
            return new Command(ID.NotNull(), Message.NotNull(), Target);
        }
    }
}

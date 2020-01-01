namespace DX.Cqrs.Commands
{

    //public class CommandHistory : AggregateRoot {
    //    private CommandHistory() { }

    //    public CommandHistory(ICommand command) {
    //        ID = command.ID;
    //        ApplyChange(new CommandQueued(command));
    //    }

    //    public void Succeeded() {
    //        ApplyChange(new CommandSucceeded());
    //    }

    //    public void Started() {
    //        ApplyChange(new CommandStarted());
    //    }


    //    public class CommandQueued : CommandEvent {
    //        // TODO: Add User

    //        public ICommand Command { get; }

    //        public CommandQueued(ICommand command) {
    //            Command = command;
    //        }
    //    }

    //    public class CommandSucceeded : CommandEvent {

    //    }

    //    public class CommandStarted : CommandEvent {

    //    }

    //    public class CommandEvent : IEvent { }
    //}
}

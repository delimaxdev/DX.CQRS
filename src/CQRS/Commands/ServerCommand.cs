using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.Domain;
using System;
using static DX.Contracts.Cqrs.Domain.IServerCommand;

namespace DX.Cqrs.Commands {
    [Stream]
    public class ServerCommand : AggregateRoot {
        private State _state;

        public ID? Target{ get; private set; }

        public ICommandMessage Message { get; private set; }

        public bool IsCompletedSuccessfully => _state == State.Succeeded;

        public ServerCommand(ID id, ID? target, ICommandMessage message) : this() {
            ID = id;
            M.ApplyChange(new CreatedBuilder { Target = target, Message = message });
        }

        private ServerCommand() {
            M.Apply<Created>(e => {
                Target = e.Target;
                Message = e.Message;
                _state = State.Created;
            });

            M.Apply<Queued>(e => _state = State.Queued);
            M.Apply<Started>(e => _state = State.Started);
            M.Apply<Succeeded>(e => _state = State.Succeeded);
            M.Apply<Failed>(e => _state = State.Failed);
        }

        public void Queued(DateTime timestamp, CommandMetadata metadata) {
            Check.Requires<InvalidOperationException>(!IsCompletedSuccessfully);
            M.ApplyChange(new QueuedBuilder { Timestamp = timestamp, Metadata = metadata });
        }

        public void BeginExecution(DateTime timestamp) {
            Check.Requires<InvalidOperationException>(!IsCompletedSuccessfully);
            M.ApplyChange(new StartedBuilder { Timestamp = timestamp });
        }

        public void EndExecution(DateTime timestamp) {
            Check.Requires<InvalidOperationException>(_state == State.Started);
            M.ApplyChange(new SucceededBuilder { Timestamp = timestamp });
        }

        public void EndExecution(DateTime timestamp, Exception ex) {
            Check.Requires<InvalidOperationException>(_state == State.Started);
            M.ApplyChange(new FailedBuilder {
                Timestamp = timestamp,
                Message = ex.Message,
                ExceptionType = ex.GetType().Name,
                ExceptionStacktrace = ex.StackTrace
            });
        }
        private enum State {
            Created, 
            Queued, 
            Started,
            Succeeded,
            Failed
        }
    }
}

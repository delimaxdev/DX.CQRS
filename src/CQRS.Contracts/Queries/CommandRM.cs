using DX.Contracts.Cqrs.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts.Cqrs.Queries {
    [Contract]
    public class CommandRM : IHasID<ID> {
        public ID ID { get; }

        public ID? Target { get; }

        public string Type { get; }

        public DateTime? RequestTime { get; set; }

        public DateTime? StartTime { get; set; }

        public TimeSpan? Duration { get; set; }

        public CommandState State { get; set; }

        public string? FailureMessage { get; set; }

        public string? ExceptionType { get; set; }

        public string? ExceptionStacktrace { get; set; }

        public Ref<IServerCommand>? Parent { get; set; }

        public List<CommandRM> Commands { get; } = new List<CommandRM>();

        public CommandRM(ID id, string type, ID? target, CommandState state) {
            ID = Check.NotNull(id, nameof(id));
            Type = Check.NotNull(type, nameof(type));
            State = state;
        }
    }

    [Contract]
    public enum CommandState {
        Created,
        Queued,
        Started,
        Succeeded,
        Failed
    }
}
using DX.Cqrs.Commands;
using System;

namespace DX.Contracts.Cqrs.Domain
{
    [ContractContainer]
    public partial interface IServerCommand : IHasID<ID> {
        [Contract]
        public partial class Created : IEvent {
            [ContractMember("T")]
            public ID? Target { get; }

            [ContractMember("Msg")]
            public ICommandMessage Message { get; }
        }

        [Contract]
        public partial class Queued : IEvent {
            [ContractMember("ts")]
            public DateTime Timestamp { get; }

            [ContractMember("MD")]
            public CommandMetadata Metadata { get; }
        }

        [Contract]
        public partial class Started : IEvent {
            [ContractMember("ts")]
            public DateTime Timestamp { get; }
        }

        [Contract]
        public partial class Succeeded : IEvent {
            [ContractMember("ts")]
            public DateTime Timestamp { get; }
        }

        [Contract]
        public partial class Failed : IEvent {
            [ContractMember("ts")]
            public DateTime Timestamp { get; }

            public string Message { get; }

            public string? ExceptionType { get; }

            public string? ExceptionStacktrace { get; }
        }
    }
}
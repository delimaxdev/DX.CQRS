using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using System;

namespace DX.Cqrs.Commands
{
    [Contract]
    public class CommandMetadata {
        [ContractMember("TS")]
        public DateTime RequestTime { get; }

        public Ref<IServerCommand>? Parent { get; }

        public CommandMetadata(DateTime requestTime, Ref<IServerCommand>? parent = null) {
            RequestTime = requestTime;
            Parent = parent;
        }
    }
}
using System;

namespace DX.Contracts.Cqrs.Domain
{
    [Contract]
    public class DefaultEventMetadata {
        [ContractMember("ts")]
        public DateTime Timestamp { get; }

        [ContractMember("c")]
        public ID? Command { get; }

        public DefaultEventMetadata(DateTime timestamp, ID? command = null) {
            Timestamp = timestamp;
            Command = command;
        }
    }
}
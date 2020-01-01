using DX.Messaging;

namespace DX.Contracts {
    [Contract(IsPolymorphic = true)]
    public interface ICommandMessage : IMessage { }
}

using DX.Contracts;
using DX.Messaging;

namespace DX.Cqrs.Domain.Core.Messaging {
    public interface IMessageDispatcher {
        void ApplyChange(IEvent @event);

        TResult Send<TResult>(IMessage<TResult> message);
    }
}

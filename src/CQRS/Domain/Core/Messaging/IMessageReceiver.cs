using DX.Contracts;
using DX.Cqrs.Commons;
using DX.Messaging;

namespace DX.Cqrs.Domain.Core.Messaging {
    public interface IMessageReceiver {
        void Receive(IEvent @event);

        Maybe<TResult> Receive<TResult>(IMessage<TResult> message);
    }
}

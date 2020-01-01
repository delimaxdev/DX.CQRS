using DX.Cqrs.Commons;

namespace DX.Messaging {
    public interface IReceivable {
        Maybe<TResult> Receive<TResult>(IMessage<TResult> message);
    }
}

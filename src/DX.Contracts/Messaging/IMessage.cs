using Nothing = DX.Cqrs.Commons.Nothing;

namespace DX.Messaging {
    public interface IMessage<out TResult> { }

    public interface IMessage : IMessage<Nothing> { }
}

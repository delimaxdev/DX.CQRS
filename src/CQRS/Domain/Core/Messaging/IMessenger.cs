namespace DX.Cqrs.Domain.Core.Messaging {
    public interface IMessenger : IMessageReceiver {
        IMessageDispatcher Dispatcher { set; }
    }
}

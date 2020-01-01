using DX.Contracts;
using DX.Cqrs.Commons;
using DX.Messaging;
using System;

namespace DX.Cqrs.Domain.Core.Messaging {
    public class MessageDispatcher : IMessageDispatcher {
        private readonly IMessenger _target;

        public MessageDispatcher(IMessenger target) {
            _target = Check.NotNull(target, nameof(target));
            _target.Dispatcher = this;
        }

        public virtual void ApplyChange(IEvent @event)
            => Dispatch(@event);

        public virtual TResult Send<TResult>(IMessage<TResult> message) {
            if (Dispatch(message) is Some<TResult> result) {
                return result;
            }

            throw new InvalidOperationException(
                $"No handler was found for message type {message.GetType().Name}.");
        }

        protected void Dispatch(IEvent @event)
            => _target.Receive(Check.NotNull(@event, nameof(@event)));

        protected Maybe<TResult> Dispatch<TResult>(IMessage<TResult> message)
            => _target.Receive(Check.NotNull(message, nameof(message)));
    }
}

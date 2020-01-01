using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DX.Messaging
{
    public static class MessagingExtensions {
        public static ISendFluent<TMessage, TResult> Send<TMessage, TResult>(this IServiceProvider services, TMessage message) where TMessage : IMessage<TResult> {
            return new SendFluent<TMessage, TResult>(services, message);
        }

        public static ISendFluent<TMessage> Send<TMessage>(this IServiceProvider services, TMessage message) where TMessage : IMessage<Nothing> {
            return new SendFluent<TMessage>(services, message);
        }

        public static void Send<TMessage>(this IReceives<TMessage> target, TMessage message) where TMessage : IMessage<Nothing> {
            Send<TMessage, Nothing>(target, message);
        }

        public static TResult Send<TMessage, TResult>(this IReceives<TMessage> target, TMessage message) where TMessage : IMessage<TResult> {
            if (target.Receive(message).Get(out TResult result)) {
                return result;
            }

            throw new InvalidOperationException(
                $"The given target object '{target}' did not handle the message '{message}'.");
        }

        public interface ISendFluent<TMessage, TResult> where TMessage : IMessage<TResult> {
            TResult To<TReceiver>() where TReceiver : IReceives<TMessage>;
        }

        public interface ISendFluent<TMessage> where TMessage : IMessage<Nothing> {
            void To<TReceiver>() where TReceiver : IReceives<TMessage>;
        }

        private class SendFluent<TMessage, TResult> :
            ISendFluent<TMessage, TResult>
            where TMessage : IMessage<TResult> {

            private readonly IServiceProvider _services;
            private readonly TMessage _message;

            public SendFluent(IServiceProvider services, TMessage message)
                => (_services, _message) = (services, message);

            public TResult To<TReceiver>() where TReceiver : IReceives<TMessage> {
                bool noReceiverRegistred = true;
                foreach (IReceivable receiver in _services.GetServices<TReceiver>()) {
                    noReceiverRegistred = false;

                    if (receiver.Receive(_message).Get(out TResult result)) {
                        return result;
                    }
                }

                if (noReceiverRegistred) {
                    throw new InvalidOperationException(
                        $"The current DI container could not resolve a service implementing " +
                        $"IReceives<{typeof(TMessage).Name}>.");
                }

                throw new InvalidOperationException(
                    $"None of the registered IReceives<{typeof(TMessage).Name}> instances did " +
                    $"process the given message of type {_message.GetType().Name}.");
            }
        }

        private class SendFluent<TMessage> :
            SendFluent<TMessage, Nothing>,
            ISendFluent<TMessage>
            where TMessage : IMessage<Nothing> {

            public SendFluent(IServiceProvider services, TMessage message)
                : base(services, message) { }

            void ISendFluent<TMessage>.To<TReceiver>()
                => To<TReceiver>();
        }
    }
}

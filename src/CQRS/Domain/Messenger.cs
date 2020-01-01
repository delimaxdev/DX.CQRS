using DX.Contracts;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain.Core.Messaging;
using DX.Messaging;
using System;
using System.Collections.Generic;

namespace DX.Cqrs.Domain {
    public class Messenger : IMessenger {
        private IMessageDispatcher _dispatcher = NullDispatcher.Instance;
        private readonly List<IMessageReceiver> _receivers = new List<IMessageReceiver>();

        IMessageDispatcher IMessenger.Dispatcher {
            set => _dispatcher = value;
        }

        public void ApplyChange<TEvent>(TEvent @event) where TEvent : IEvent
            => _dispatcher.ApplyChange(@event);

        public void Send<TMessage>(TMessage message) where TMessage : IMessage
            => _dispatcher.Send(message);

        public TResult Send<TMessage, TResult>(TMessage message) where TMessage : IMessage<TResult>
            => _dispatcher.Send(message);

        public void Apply<TEvent>(Action<TEvent> action) where TEvent : IEvent
            => _receivers.Add(new EventRegistration<TEvent>(Check.NotNull(action, nameof(action))));

        public void Handle<TMessage>(Action<TMessage> method) where TMessage : IMessage
            => _receivers.Add(new MessageRegistration<TMessage>(Check.NotNull(method, nameof(method))));

        public void Handle<TMessage, TResult>(Func<TMessage, TResult> method) where TMessage : IMessage<TResult>
            => _receivers.Add(new MessageRegistration<TMessage, TResult>(Check.NotNull(method, nameof(method))));

        public void Register(IMessenger receiver) {
            receiver.Dispatcher = _dispatcher;
            _receivers.Add(receiver);
        }

        void IMessageReceiver.Receive(IEvent @event) {
            foreach (IMessageReceiver receiver in _receivers) {
                receiver.Receive(@event);
            }
        }

        Maybe<TResult> IMessageReceiver.Receive<TResult>(IMessage<TResult> message) {
            foreach (IMessageReceiver receiver in _receivers) {
                if (receiver.Receive(message) is Some<TResult> result) {
                    return result;
                }
            }

            return None<TResult>.Value;
        }

        private class EventRegistration<TEvent> : IMessageReceiver {
            private readonly Action<TEvent> _action;

            public EventRegistration(Action<TEvent> action)
                => _action = action;

            public void Receive(IEvent @event) {
                if (@event is TEvent e) {
                    _action(e);
                }
            }

            public Maybe<TResult> Receive<TResult>(IMessage<TResult> message)
                => None<TResult>.Value;
        }

        private class MessageRegistration<TMessage, TResult> : IMessageReceiver where TMessage : IMessage<TResult> {
            private readonly Func<TMessage, TResult> _method;

            public MessageRegistration(Func<TMessage, TResult> method)
                => _method = method;

            public void Receive(IEvent @event) { }

            public Maybe<T> Receive<T>(IMessage<T> message) {
                if (message is TMessage m) {
                    return (T)(object)_method(m)!;
                }

                return None<T>.Value;
            }
        }

        private class MessageRegistration<TMessage> : IMessageReceiver where TMessage : IMessage<Nothing> {
            private readonly Action<TMessage> _method;

            public MessageRegistration(Action<TMessage> method)
                => _method = method;

            public void Receive(IEvent @event) { }

            public Maybe<T> Receive<T>(IMessage<T> message) {
                if (message is TMessage m) {
                    _method(m);
                }

                return None<T>.Value;
            }
        }

        private class NullDispatcher : IMessageDispatcher {
            public static readonly NullDispatcher Instance = new NullDispatcher();

            public void ApplyChange(IEvent @event)
                => throw DispatcherNotSetException();

            public TResult Send<TResult>(IMessage<TResult> message)
                => throw DispatcherNotSetException();

            private InvalidOperationException DispatcherNotSetException() =>
                throw new InvalidOperationException("IMessenger.Dispatcher has not been set. " +
                    "Make sure that the object owning this Messenger is part some aggregate " +
                    "(e.g. AggregateRoot).");
        }
    }

    public static class MessengerExtensions {
        // We cannot put this method directly in the 'Messenger' class because C#'s overload
        // resolution is not smart enough
        public static void ApplyChange<TBuilder>(this Messenger m, TBuilder builder) where TBuilder : IBuilds<IEvent>
            => m.ApplyChange(builder.Build());
    }
}

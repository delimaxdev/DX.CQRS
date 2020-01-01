using DX.Cqrs.Commons;
using System;
using System.Collections.Generic;

namespace DX.Messaging {
    public class MessageHandler : MessageHandler.ILinkedListNode {
        private List<Registration>? _registrations;
        private MessageHandler? _next;

        public virtual Maybe<TResult> Receive<TResult>(IMessage<TResult> message) {
            if (_registrations != null) {
                foreach (Registration reg in _registrations) {
                    if (reg.Handle(message) is Some<TResult> result) {
                        return result;
                    }
                }
            }

            return Next(message);
        }

        protected void Register<TMessage>(Action<TMessage> method) where TMessage : IMessage<Nothing> {
            _registrations ??= new List<Registration>();
            _registrations.Add(new Registration<TMessage>(method));
        }

        protected void Register<TMessage, TResult>(Func<TMessage, TResult> method) where TMessage : IMessage<TResult> {
            _registrations ??= new List<Registration>();
            _registrations.Add(new Registration<TMessage, TResult>(method));
        }
        protected void Register<TMessage, TResult>(Func<TMessage, Maybe<TResult>> method) where TMessage : IMessage<TResult> {
            _registrations ??= new List<Registration>();
            _registrations.Add(new MaybeRegistration<TMessage, TResult>(method));
        }

        protected Maybe<TResult> Next<TResult>(IMessage<TResult> message) {
            if (_next == null)
                return None<TResult>.Value;

            return _next.Receive(message);
        }

        void ILinkedListNode.InsertAfter(MessageHandler handler) {
            handler._next = _next;
            _next = handler;
        }

        protected interface ILinkedListNode {
            void InsertAfter(MessageHandler handler);
        }

        private abstract class Registration {
            public abstract Maybe<T> Handle<T>(IMessage<T> message);
        }

        private class Registration<TMessage, TResult> : Registration where TMessage : IMessage<TResult> {
            private readonly Func<TMessage, TResult> _method;

            public Registration(Func<TMessage, TResult> method)
                => _method = method;

            public override Maybe<T> Handle<T>(IMessage<T> message) {
                if (message is TMessage m) {
                    return (T)(object)_method(m)!;
                }

                return None<T>.Value;
            }
        }

        private class MaybeRegistration<TMessage, TResult> : Registration where TMessage : IMessage<TResult> {
            private readonly Func<TMessage, Maybe<TResult>> _method;

            public MaybeRegistration(Func<TMessage, Maybe<TResult>> method)
                => _method = method;

            public override Maybe<T> Handle<T>(IMessage<T> message) {
                if (message is TMessage m) {
                    return (Maybe<T>)(object)_method(m)!;
                }

                return None<T>.Value;
            }
        }

        private class Registration<TMessage> : Registration where TMessage : IMessage<Nothing> {
            private readonly Action<TMessage> _method;

            public Registration(Action<TMessage> method)
                => _method = method;

            public override Maybe<T> Handle<T>(IMessage<T> message) {
                if (message is TMessage m) {
                    _method(m);
                }

                return None<T>.Value;
            }
        }
    }
}

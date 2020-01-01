using DX.Contracts;
using DX.Cqrs.Common;
using DX.Cqrs.EventStore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public abstract class Query<TState> {
        private readonly List<EventRegistration> _handlers = new List<EventRegistration>();
        private readonly IEventStore _store;

        public Query(IEventStore store) {
            _store = Check.NotNull(store, nameof(store));
        }

        protected void On<TEvent>(Action<TState, TEvent, ID> action) where TEvent : IEvent {
            Check.NotNull(action, nameof(action));
            _handlers.Add(new EventRegistration<TEvent>(action));
        }

        protected void On<TEvent>(Action<TState, TEvent> action) where TEvent : IEvent {
            On<TEvent>((s, e, id) => action(s, e));
        }

        protected void For<TParent>(Action<HandlerConfigurator<TParent>> config) where TParent : IHasID<ID> {
            For((_, __) => true, config);
        }

        protected void For<TParent>(Func<TState, Ref<TParent>, bool> condition, Action<HandlerConfigurator<TParent>> config) where TParent : IHasID<ID> {
            var c = new HandlerConfigurator<TParent>(this, condition);
            config(c);
        }

        protected async Task<TState> Run(TState initialState, IContext context) {
            TState s = initialState;
            IEventCriteria criteria = _store.CreateCriteria(_ => { });
            IEventStoreTransaction tx = context
                .Get<IServiceProvider>()
                .GetRequiredService<IEventStoreTransaction>();
            
            IAsyncEnumerable<RecordedEvent> events = await tx.Get(criteria);

            await events.ForEach(re => {
                Handle(s, re);
                return Task.CompletedTask;
            });

            return s;
        }

        protected class HandlerConfigurator<TParent> where TParent : IHasID<ID> {
            private readonly Query<TState> _query;
            private readonly Func<TState, Ref<TParent>, bool> _condition;

            internal HandlerConfigurator(Query<TState> query, Func<TState, Ref<TParent>, bool> condition) {
                _condition = condition;
                _query = query;
            }

            public void On<TEvent>(Action<TState, TEvent, Ref<TParent>> action) where TEvent : IEvent<TParent> {
                _query.On<TEvent>((s, e, id) => {
                    Ref<TParent> r = id.ToRef<TParent>();
                    if (_condition(s, r)) {
                        action(s, e, r);
                    }
                });
            }

            public void On<TEvent>(Action<TState, TEvent> action) where TEvent : IEvent<TParent> {
                _query.On<TEvent>((s, e, id) => {
                    Ref<TParent> r = id.ToRef<TParent>();
                    if (_condition(s, r)) {
                        action(s, e);
                    }
                });
            }
        }

        private void Handle(TState s, RecordedEvent e) {
            foreach (EventRegistration handler in _handlers) {
                handler.Handle(s, e);
            }
        }

        private abstract class EventRegistration {
            public abstract void Handle(TState s, RecordedEvent e);
        }

        private class EventRegistration<TEvent> : EventRegistration {
            private readonly Action<TState, TEvent, ID> _action;

            public EventRegistration(Action<TState, TEvent, ID> action)
                => _action = action;

            public override void Handle(TState s, RecordedEvent e) {
                if (e.Event is TEvent ev) {
                    _action(s, ev, (ID)e.StreamID);
                }
            }
        }
    }
}
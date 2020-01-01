using DX.Contracts;
using DX.Contracts.Cqrs.Queries;
using DX.Cqrs.Common;
using DX.Cqrs.Domain.Core;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public class EventsQuery : Query<EventsQuery.State>, ICollectionQuery<GetEvents, IEvent> {
        public EventsQuery(IEventStore store) : base(store) {
            On<IEvent>((s, e, id) => {
                if (id == s.StreamID) {
                    s.Events.Add(e);
                }
            });
        }

        public async Task<IReadOnlyCollection<IEvent>> Run(GetEvents criteria, IContext context)
            => (await Run(new State(criteria.Object), context)).Events;

        public class State {
            public ID StreamID { get; }

            public List<IEvent> Events { get; } = new List<IEvent>();

            public State(Ref<IHasID<ID>> @object) {
                StreamID = ID.FromRef(@object);
            }
        }
    }
}
using DX.Contracts;
using DX.Cqrs.Domain.Core.Messaging;
using System.Collections.Generic;

namespace DX.Cqrs.Domain {
    public class Restorable {
        private readonly ChangeTrackingDispatcher _dispatcher;

        public Restorable() {
            _dispatcher = new ChangeTrackingDispatcher(M);
        }

        protected Messenger M { get; } = new Messenger();

        public void Restore(IEnumerable<IEvent> events) {
            _dispatcher.Restore(events);
        }
    }
}

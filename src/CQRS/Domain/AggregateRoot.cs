using DX.Contracts;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain.Core;
using DX.Cqrs.Domain.Core.Messaging;
using DX.Messaging;
using System.Collections.Generic;

namespace DX.Cqrs.Domain {
    public class AggregateRoot : DomainObject, IPersistable, IReceivable {
        private ChangeTrackingDispatcher _dispatcher;

        protected ID ID { get; set; }

        ID IHasID<ID>.ID => ID;

        public AggregateRoot() : base(new Messenger()) {
            _dispatcher = new ChangeTrackingDispatcher(M);
        }

        void IPersistable.ClearChanges() {
            _dispatcher.ClearChanges();
        }

        Changeset IPersistable.GetChanges() {
            return _dispatcher.GetChanges();
        }

        void IPersistable.Restore(ID id, IEnumerable<IEvent> events) {
            ID = id;
            _dispatcher.Restore(events);
        }

        Maybe<TResult> IReceivable.Receive<TResult>(IMessage<TResult> message) {
            IMessageReceiver m = M;
            return m.Receive(message);
        }

        public override string ToString() {
            return $"{this.GetType().Name} {ID}";
        }
    }
}

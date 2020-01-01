using DX.Contracts;
using System.Collections.Generic;

namespace DX.Cqrs.Domain.Core.Messaging {
    public class ChangeTrackingDispatcher : MessageDispatcher {
        private readonly List<IEvent> _changes = new List<IEvent>();
        private bool _isNew = true;

        public ChangeTrackingDispatcher(IMessenger target)
            : base(target) { }

        public override void ApplyChange(IEvent @event) {
            _changes.Add(@event);
            base.ApplyChange(@event);
        }

        public void ClearChanges() {
            _changes.Clear();
            _isNew = false;
        }

        public Changeset GetChanges() {
            return new Changeset(_changes, _isNew);
        }

        public void Restore(IEnumerable<IEvent> events) {
            foreach (IEvent e in events) {
                Dispatch(e);
            }

            _isNew = false;
        }
    }
}

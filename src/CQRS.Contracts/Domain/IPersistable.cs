using DX.Contracts;
using System.Collections.Generic;

namespace DX.Cqrs.Domain.Core {
    public interface IPersistable : IHasID<ID> {
        Changeset GetChanges();

        void ClearChanges();

        void Restore(ID id, IEnumerable<IEvent> events);
    }

    public class Changeset {
        public bool IsNew { get; }

        public IReadOnlyCollection<IEvent> Changes { get; }

        public Changeset(IReadOnlyCollection<IEvent> changes, bool isNew)
            => (Changes, IsNew) = (Check.NotNull(changes, nameof(changes)), isNew);
    }
}

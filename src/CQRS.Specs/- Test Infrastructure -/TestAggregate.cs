using DX.Contracts;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using DX.Messaging;
using System.Collections.Generic;

namespace DX.Testing {
    public class TestAggregate : AggregateRoot {
        public new ID ID => base.ID;

        public TestAggregate() {
            base.ID = ID.NewID();
        }

        public TestAggregate(ID id) {
            base.ID = id;
        }

        public void ApplyChange(IEvent e) {
            M.ApplyChange(e);
        }
    }
}
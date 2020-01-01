using DX.Contracts.ReadModels;
using DX.Cqrs.Domain.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts.Cqrs.Queries {
    [Contract]
    public class GetEvents : ICollectionCriteria<IEvent> {
        public Ref<IHasID<ID>> Object { get; }

        public GetEvents(Ref<IHasID<ID>> @object)
            => Object = Check.NotNull(@object, nameof(@object));
    }
}
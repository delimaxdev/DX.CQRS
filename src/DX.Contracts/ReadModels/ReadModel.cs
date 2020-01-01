using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts.ReadModels {
    public abstract class ReadModel<TRef> : IReadModel, IHasID<ID>
        where TRef : IHasID<ID> {
        private readonly Ref<TRef> _reference;

        public ID ID => ID.FromRef(_reference);

        protected ReadModel(Ref<TRef> reference)
            => _reference = Check.NotNull(reference, nameof(reference));

        public static implicit operator Ref<TRef>(ReadModel<TRef> rm) {
            Check.NotNull(rm, nameof(rm));
            return rm._reference;
        }
    }
}
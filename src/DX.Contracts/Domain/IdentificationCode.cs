using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts.Domain {
    [Contract(IsPolymorphic = true)]
    public abstract class IdentificationCode : IIdentifier {
        public override bool Equals(object? other)
            => other is IdentificationCode code && EqualsCore(code);

        public override int GetHashCode()
            => GetHashCodeCore();

        protected abstract bool EqualsCore(IdentificationCode other);

        protected abstract int GetHashCodeCore();

        public static bool operator ==(IdentificationCode? left, IdentificationCode? right)
            => Equals(left, right);

        public static bool operator !=(IdentificationCode? left, IdentificationCode? right)
            => !Equals(left, right);
    }
}
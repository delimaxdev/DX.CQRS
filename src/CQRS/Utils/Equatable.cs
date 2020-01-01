using System;

namespace DX.Cqrs.Commons
{
    public abstract class Equatable<T> where T : class {

        public override bool Equals(object other) {
            if (other is T t)
                return EqualsCore(t);

            return false;
        }

        public override int GetHashCode() {
            return GetHashCodeCore();
        }
        
        public static bool operator ==(Equatable<T> left, Equatable<T> right) {
            return Object.Equals(left, right);
        }

        public static bool operator !=(Equatable<T> left, Equatable<T> right) {
            return !Object.Equals(left, right);
        }

        protected abstract bool EqualsCore(T other);

        protected abstract int GetHashCodeCore();
    }

    public abstract class Equatable<T, TValue> : Equatable<T> where T : Equatable<T, TValue> {
        protected TValue Value { get; }

        protected Equatable(TValue value)
            => Value = value;

        protected override int GetHashCodeCore() {
            return HashCode.Combine(Value);
        }

        protected override bool EqualsCore(T other) {
            return Object.Equals(Value, other.Value);
        }

        public override string ToString() {
            if (Value is null) {
                return "<NULL>";
            }

            return Value.ToString();
        }
    }
}

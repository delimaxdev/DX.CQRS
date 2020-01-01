using System;
using System.Collections.Generic;

namespace DX.Cqrs.Commons {
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

    /// <summary>
    ///     Encapsulates an optional value and is optimized for C#'s new pattern matching capabilities.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[Maybe<Employee> GetEmployee(int number) {
    ///     Employee e = DB.GetEmployee(number);
    ///     
    ///     return e.NoneIfNull();
    /// }]]>
    /// </code>
    /// <code>
    /// <![CDATA[Maybe<Employee> result = GetEmployee(1234);
    /// if (result is Some<Employee> value) {
    ///     // You can use value just as you would use an actual Employee variable
    ///     Employee emp = value;
    ///     emp.AssignProject(1337);
    /// }]]>
    /// </code>
    /// </example>
    public abstract class Maybe<T> {
        [Obsolete("Use Get")]
        public virtual T Value 
            => throw new InvalidOperationException("This Maybe<T> instance does not hold a value.");

        public static bool operator ==(Maybe<T> left, Maybe<T> right) {
            return Object.Equals(left, right);
        }

        public static bool operator !=(Maybe<T> left, Maybe<T> right) {
            return !(left == right);
        }

        public static implicit operator Maybe<T>(T value) {
            return new Some<T>(value);
        }

        public abstract T OrDefault();

        public abstract T OrDefault(T defValue);

        public abstract T OrDefault(Func<T> defaultValueFactory);

        public T Get() => Value;

        public abstract bool Get(out T value);

        public abstract bool Is<U>(out U value) where U : T;
    }

    public static class Maybe {
        public static Maybe<T> NoneIfNull<T>(this T? value) where T : class {
            if (value == null) {
                return None<T>.Value;
            }

            return value;
        }

        public static Maybe<TValue> GetValueMaybe<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key) {
            TValue value;
            bool found = false;

            switch (dictionary) {
                case IDictionary<TKey, TValue> d:
                    found = d.TryGetValue(key, out value);
                    break;
                case IReadOnlyDictionary<TKey, TValue> d:
                    found = d.TryGetValue(key, out value);
                    break;
                default:
                    throw new ArgumentException(
                        "The given collection must implement either IDictionary<,> or IReadOnlyDictionary<,>",
                        nameof(dictionary)
                    );
            }

            return found ? value : None<TValue>.Value;
        }
    }

    public sealed class Some<T> : Maybe<T> {
        private readonly T _value;

        internal Some(T value) {
            _value = value;
        }

        public override T Value => _value;

        public override bool Equals(object other) {
            return
                other is Some<T> otherValue &&
                EqualityComparer<T>.Default.Equals(otherValue._value, _value);
        }

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(_value);
        }

        public override T OrDefault() {
            return _value;
        }

        public override T OrDefault(T defValue) {
            return _value;
        }

        public override T OrDefault(Func<T> defaultValueFactory) {
            return _value;
        }

        public static implicit operator Some<T>(T value) {
            return new Some<T>(value);
        }

        public static implicit operator T(Some<T> value) {
            return value._value;
        }

        public override string ToString() {
            return _value != null ?
                _value.ToString() :
                "null";
        }

        public override bool Get(out T value) {
            value = _value;
            return true;
        }

        public override bool Is<U>(out U value) {
            if (_value is U val) {
                value = val;
                return true;
            }

            value = default!;
            return false;
        }
    }

    public sealed class None<T> : Maybe<T> {
        public static new readonly Maybe<T> Value = new None<T>();

        private None() { }

        public override bool Get(out T value) {
            value = default!;
            return false;
        }

        public override bool Is<U>(out U value) {
            value = default!;
            return false;
        }

#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.

        public override T OrDefault() {
            return default;
        }

#pragma warning restore CS8653

        public override T OrDefault(T defValue) {
            return defValue;
        }

        public override T OrDefault(Func<T> defaultValueFactory) {
            return defaultValueFactory();
        }

        public override string ToString() {
            return "None";
        }
    }
}

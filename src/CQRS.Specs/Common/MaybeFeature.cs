using DX.Cqrs.Commons;
using DX.Testing;
using FluentAssertions;
using Xbehave;

namespace Common {
    public class MaybeFeature : Feature {
        [Scenario]
        public void MaybeTests(
            Maybe<SomeClass> first,
            Maybe<SomeClass> second,
            Some<SomeClass> firstValue,
            Some<SomeClass> secondValue
        ) {
            SomeClass value1 = new SomeClass();
            SomeClass value2 = new SomeClass();

            GIVEN["two None<T> values"] = () => (first, second) = (None<SomeClass>.Value, None<SomeClass>.Value);
            THEN["Equals returns true"] = () => first.Equals(second).Should().BeTrue();
            THEN["== returns true"] = () => (first == second).Should().BeTrue();

            GIVEN["two equal Maybe<T>"] = () => (first, second) = (value1, value1);
            THEN["Equals returns true"] = () => first.Equals(second).Should().BeTrue();
            THEN["== returns true"] = () => (first == second).Should().BeTrue();

            GIVEN["two different Maybe<T>"] = () => (first, second) = (value1, value2);
            THEN["Equals returns false"] = () => first.Equals(second).Should().BeFalse();
            THEN["== returns false"] = () => (first == second).Should().BeFalse();

            GIVEN["a None<T> value and a Some<T>"] = () => (first, second) = (None<SomeClass>.Value, value2);
            THEN["Equals returns false"] = () => {
                first.Equals(second).Should().BeFalse();
                second.Equals(first).Should().BeFalse();
            };
            THEN["== returns false"] = () => {
                (first == second).Should().BeFalse();
                (second == first).Should().BeFalse();
            };

            GIVEN["two equal values typed as Some<T>"] = () => (firstValue, secondValue) = (value1, value1);
            THEN["Equals returns true"] = () => firstValue.Equals(secondValue).Should().BeTrue();
            THEN["== returns true"] = () => (firstValue == secondValue).Should().BeTrue();

            GIVEN["an Maybe<T> value"] = () => first = value1;
            THEN["value should be implicitly castable"] = () => {
                (first is Some<SomeClass>).Should().BeTrue();
                if (first is Some<SomeClass> v) {
                    SomeClass casted = v;
                    casted.Should().Be(value1);
                }
            };

            GIVEN["a None<T> value"] = () => first = None<SomeClass>.Value;
            THEN["OrDefault returns default"] = () => {
                var defValue = new SomeClass();
                first.OrDefault(defValue).Should().Be(defValue);
                first.OrDefault(() => defValue).Should().Be(defValue);
                first.OrDefault().Should().Be(null);

            };

            GIVEN["a Some<T>"] = () => first = value1;
            THEN["OrDefault returns the actual value"] = () => {
                var defValue = new SomeClass();
                first.OrDefault(defValue).Should().Be(value1);
                first.OrDefault(() => defValue).Should().Be(value1);
                first.OrDefault().Should().Be(value1);

            };

            GIVEN["an instance"] = () => value1 = new SomeClass();
            WHEN["calling NoneIfNull"] = () => first = value1.NoneIfNull();
            THEN["the result is a Some<T>"] = () => first.Should().Be((Some<SomeClass>)value1);

            GIVEN["an null reference"] = () => value1 = null;
            WHEN["calling NoneIfNull"] = () => first = value1.NoneIfNull();
            THEN["the result is a None<T>"] = () => first.Should().Be(None<SomeClass>.Value);
        }

        public class SomeClass { }
    }
}

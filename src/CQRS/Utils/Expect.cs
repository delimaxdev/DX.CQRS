using System;
using System.Collections.Generic;
using System.Linq;

namespace DX {
    internal static class Expect {
        public static void That(bool condition, string? message = null, params object[] messageArgs) {
            if (!condition) {
                throw new AssertionException($"Assertion failed.", message, messageArgs);
            }
        }

        public static void IsPositiveOrZero(int number, string? message = null, params object[] messageArgs) {
            if (number < 0) {
                throw new AssertionException($"Expected value to be positive or zero but was {number}.", message, messageArgs);
            }
        }

        public static T IsNotNull<T>(T? value, string? message = null, params object[] messageArgs) where T: class {
            if (value == null) {
                throw new AssertionException("Expected value to be not null.", message, messageArgs);
            }

            return value;
        }

        public static string IsNotEmpty(string? value, string? message = null, params object[] messageArgs) {
            if (String.IsNullOrEmpty(value)) {
                throw new AssertionException("Expected string to be not empty.", message, messageArgs);
            }

            return value;
        }

        public static void IsNotEmpty<T>(IEnumerable<T> collection, string? message = null, params object[] messageArgs) {
            if (!collection.Any()) {
                throw new AssertionException("Expected collection to be not empty.", message, messageArgs);
            }
        }

        public static T IsOfType<T>(object? value, string? message = null, params object[] messageArgs) {
            if (Expect.IsNotNull(value) is T typedValue) {
                return typedValue;
            }

            throw new AssertionException($"Expected value to be of type {typeof(T).Name}.", message, messageArgs);
        }
    }

    public class AssertionException : Exception {
        public AssertionException(string message) : base(message) { }

        public AssertionException(string defaultMessage, string? message, object[] messageArgs) :
            base(GetMessage(defaultMessage, message, messageArgs)) { }

        private static string GetMessage(string defaultMessage, string? message, object[] messageArgs) {
            if (String.IsNullOrEmpty(message))
                return defaultMessage;

            return String.Format(message, messageArgs);
        }
    }
}
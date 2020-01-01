using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DX {
    /// <summary>
    ///   Provides static methods for parameter checks (similar to code contracts).
    /// </summary>
    /// <remarks>
    ///   Use 'nameof' operator for 'parameterName' parameters!
    /// </remarks>
    [DebuggerStepThrough]
    internal static class Check {
        /// <summary>
        ///   Verifies that the supplied value is not a null reference.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <param name="parameterName">Use 'nameof' operator!</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotNull<T>(T value, string parameterName) {
            if (value == null) {
                BreakIfDebugging();

                // ArgumentNullException is not used here because the method might also be used to check
                // properties on arguments instead of the argument itself.
                throw new ArgumentException("The argument or a property on the argument cannot be null.", parameterName);
            }

            return value;
        }

        public static T IsOfType<T>(object value, string parameterName) {
            NotNull<object>(value, parameterName);

            if (value is T typed)
                return typed;
            
            BreakIfDebugging();

            // ArgumentNullException is not used here because the method might also be used to check
            // properties on arguments instead of the argument itself.
            throw new ArgumentException(
                $"Expected argument or a property on the argument to be of type '{typeof(T).Name}'.",
                parameterName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NotNegative(int value, string parameterName) {
            if (value < 0) {
                BreakIfDebugging();
                throw new ArgumentOutOfRangeException(parameterName, value, "The argument cannot be negative.");
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Positive(int value, string parameterName) {
            if (value <= 0) {
                BreakIfDebugging();
                throw new ArgumentOutOfRangeException(parameterName, value, "The argument cannot be 0 or negative.");
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NotNegative(long value, string parameterName) {
            if (value < 0) {
                BreakIfDebugging();
                throw new ArgumentOutOfRangeException(parameterName, value, "The argument cannot be negative.");
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Within(int value, int min, int max, string parameterName) {
            if (value < min || value > max) {
                BreakIfDebugging();
                throw new ArgumentOutOfRangeException(parameterName, value, $"The argument must be between {min} and {max}.");
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Within(long value, long min, long max, string parameterName) {
            if (value < min || value > max) {
                BreakIfDebugging();
                throw new ArgumentOutOfRangeException(parameterName, value, $"The argument must be between {min} and {max}.");
            }

            return value;
        }

        /// <summary>
        ///   Verifies that the supplied value is not a null reference.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that will be thrown.</typeparam>
        /// <remarks>
        ///   Can be used to check something that isn't an argument (e.g. with <see cref="InvalidOperationException"/>).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<TException>(object? value, string? message = null) where TException : Exception {
            if (value == null) {
                BreakIfDebugging();
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        ///   Verifies that the supplied string value is neither a null reference nor an empty string.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is null or an empty string.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NotEmpty(string value, string parameterName) {
            if (String.IsNullOrEmpty(value)) {
                BreakIfDebugging();
                throw new ArgumentException("The argument or a property on the argument cannot be null or an empty string.", parameterName);
            }

            return value;
        }

        /// <summary>
        ///   Verifies that the supplied enumerable is neither a null reference nor empty.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is null or an empty enumerable.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotEmpty<T>(T value, string parameterName) where T : IEnumerable {
            if (value == null || !value.GetEnumerator().MoveNext()) {
                BreakIfDebugging();
                throw new ArgumentException("The argument or a property on the argument cannot be null or an empty enumerable.", parameterName);
            }

            return value;
        }

        /// <summary>
        ///   Verifies that the supplied value is not the default value of the type <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> equals the default value of <typeparamref name="T"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotDefault<T>(T value, string parameterName) where T : struct {
            if (value.Equals(default(T))) {
                BreakIfDebugging();
                throw new ArgumentException("The argument or a property on the argument cannot have the default value.", parameterName);
            }
        }

        /// <summary>
        ///   Verifies that the supplied boolean value is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(bool condition, string? parameterName = null, string? message = null, params object[] messageArgs) {
            if (!condition) {
                BreakIfDebugging();
                throw new ArgumentException(FormatMessage(message, parameterName, messageArgs), parameterName);
            }
        }

        /// <summary>
        ///   Verifies that the supplied boolean value is true.
        /// </summary>
        /// <remarks>
        ///   Can be used to check something that isn't an argument (e.g. with <see cref="InvalidOperationException"/>).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires<TException>(bool condition, string? message = null, params object[] messageArgs) where TException : Exception {
            if (!condition) {
                BreakIfDebugging();
                throw (TException)Activator.CreateInstance(typeof(TException), FormatMessage(message, null, messageArgs));
            }
        }

        private static string? FormatMessage(string? message, string? parameterName, params object[] messageArgs) {
            if (message == null)
                return null;

            if (parameterName != null) {
                message = message.Replace("{param}", parameterName);
            }

            return String.Format(message, messageArgs);
        }

        /// <summary>
        ///   Breaks execution if a debugger is attached. Only compiled in debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        private static void BreakIfDebugging() {
            if (Debugger.IsAttached) {
                Debugger.Break();
            }
        }
    }
}
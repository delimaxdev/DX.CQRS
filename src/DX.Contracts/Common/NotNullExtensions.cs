namespace DX {
    public static class ValueTypeExtensions {
        public static T NotNull<T>(this T? value) where T : struct {
            if (!value.HasValue) {
                throw new AssertionException($"Expected Nullable<{typeof(T).Name}> to have a value.");
            }

            return value.Value;
        }
    }

    public static class ReferenceTypeExtensions {
        public static T NotNull<T>(this T? value) where T : class {
            if (value == null) {
                throw new AssertionException($"Expected expression of type <{typeof(T).Name}> to be not null.");
            }

            return value;
        }
    }
}

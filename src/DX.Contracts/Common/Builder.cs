using System.Diagnostics;

namespace DX.Contracts {
    public static class Builder {
        [Conditional("DEBUG")]
        public static void Set(object? property, object? value) { }
    }
}
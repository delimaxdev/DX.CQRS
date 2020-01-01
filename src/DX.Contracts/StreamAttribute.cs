using System;

namespace DX.Contracts {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StreamAttribute : Attribute {
        public string? Name { get; set; }

        public StreamAttribute() { }

        public StreamAttribute(string name) {
            Name = Check.NotNull(name, nameof(name));
        }
    }
}

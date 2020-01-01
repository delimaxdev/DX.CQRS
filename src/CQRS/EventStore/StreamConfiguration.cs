using System;

namespace DX.Cqrs.EventStore {
    public class StreamConfiguration {
        public Type Type { get; }

        public string Name { get; }

        public StreamConfiguration(Type type, string name) {
            Type = Check.NotNull(type, nameof(type));
            Name = Check.NotEmpty(name, nameof(name));
        }
    }
}

using DX.Contracts;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DX.Cqrs.Domain.Core {
    public class StreamConfigurationBuilder {
        private readonly List<StreamConfiguration> _streams = new List<StreamConfiguration>();

        public StreamConfigurationBuilder() {
            Scan(GetType().Assembly);
        }

        public void Scan(Assembly assembly) {
            foreach (Type type in assembly.GetTypes()) {
                StreamAttribute attribute = type.GetCustomAttribute<StreamAttribute>(inherit: false);

                if (attribute != null) {
                    string name = attribute.Name ?? type.Name;
                    _streams.Add(new StreamConfiguration(type, name));
                }
            }
        }

        public IReadOnlyCollection<StreamConfiguration> Build()
            => _streams;
    }
}

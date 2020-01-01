using System;
using System.Collections;
using System.Collections.Generic;

namespace DX.Cqrs.EventStore.Mongo
{
    internal sealed class StreamTypeRegistry : IEnumerable<MongoStreamConfiguration> {
        private readonly Dictionary<Type, MongoStreamConfiguration> _types = new Dictionary<Type, MongoStreamConfiguration>();

        public MongoStreamConfiguration this[Type streamType]
            => _types[streamType];

        public StreamTypeRegistry(Dictionary<Type, MongoStreamConfiguration> streamTypes) {
            _types = streamTypes;
        }

        public bool Contains(Type streamType) {
            return _types.ContainsKey(streamType);
        }

        public bool TryGetConfiguration(Type streamType, out MongoStreamConfiguration config) {
            return _types.TryGetValue(streamType, out config);
        }

        public IEnumerator<MongoStreamConfiguration> GetEnumerator() {
            return _types.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    internal sealed class MongoStreamConfiguration {
        public string Name { get; }

        public string StreamInfoName => $"{Name}_Info";

        public MongoStreamConfiguration(string name)
            => Name = name;
    }
}

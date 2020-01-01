using DX.Contracts.Serialization;
using System;

namespace DX.Cqrs.EventStore
{
    public class TypeNameResolver : ITypeNameResolver {
        private readonly SerializationTypeRegistry _types;

        public TypeNameResolver(SerializationTypeRegistry types)
            => _types = types;

        public string GetTypeName(Type type)
            => ((ContractTypeInfo)_types.GetInfo(type)).Discriminator;
    }
}
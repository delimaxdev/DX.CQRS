using System;

namespace DX.Contracts {
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum, 
        AllowMultiple = false, 
        Inherited = false)]
    public sealed class ContractAttribute : Attribute {
        public string? Name { get; set; }

        public string? PartialName { get; set; }

        public Type[] GenericArguments { get; set; } = new Type[0];

        public bool IsPolymorphic { get; set; } = false;

        public ContractAttribute() { }

        public ContractAttribute(string name) {
            Name = Check.NotNull(name, nameof(name));
        }
    }
}
using System;

namespace DX.Contracts {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ContractContainerAttribute : Attribute {
        public string? Name { get; set; }

        public ContractContainerAttribute() { }

        public ContractContainerAttribute(string name)
            => Name = Check.NotEmpty(name, nameof(name));
    }
}
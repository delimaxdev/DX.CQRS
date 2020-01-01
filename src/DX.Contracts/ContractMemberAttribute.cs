using System;

namespace DX.Contracts {
    [AttributeUsage(
        AttributeTargets.Property, 
        AllowMultiple = false, 
        Inherited = false)]
    public sealed class ContractMemberAttribute : Attribute {
        public string? Name { get; set; }

        public ContractMemberAttribute() { }

        public ContractMemberAttribute(string name) {
            Name = Check.NotNull(name, nameof(name));
        }
    }
}
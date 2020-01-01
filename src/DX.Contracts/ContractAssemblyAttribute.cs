using System;

namespace DX.Contracts
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ContractAssemblyAttribute : Attribute {
        public string ModuleCode { get; }

        public ContractAssemblyAttribute(string moduleCode)
            => ModuleCode = Check.NotNull(moduleCode, nameof(moduleCode));
    }
}

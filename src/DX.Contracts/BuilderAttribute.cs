using System;

namespace DX.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuilderAttribute : Attribute {
        public Type BuilderType { get; }

        public BuilderAttribute(Type builderType)
            => BuilderType = Check.NotNull(builderType, nameof(builderType));
    }
}

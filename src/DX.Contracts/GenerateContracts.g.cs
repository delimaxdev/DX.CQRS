#nullable enable
namespace DX.Cqrs.Commands
{
    using System;
    using System.Collections.Generic;
    using DX;
    using DX.Contracts;
    using DX.Contracts;

    [Builder(typeof(RunScriptBuilder))]
    partial class RunScript
    {
        public RunScript(RunScriptBuilder source): base()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Script = source.Script.NotNull();
        }

        public RunScript(RunScript source): base()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Script = source.Script;
        }
    }

    public partial class RunScriptBuilder : IBuilds<RunScript>
    {
        public Script? Script
        {
            get;
            set;
        }

        public RunScript Build()
        {
            return new RunScript(this);
        }

        public static implicit operator RunScript(RunScriptBuilder builder)
        {
            return builder.Build();
        }

        public RunScriptBuilder(RunScript? source = null): base()
        {
            if (source != null)
            {
                Script = source.Script;
            }
        }

        public RunScriptBuilder(): base()
        {
        }
    }
}

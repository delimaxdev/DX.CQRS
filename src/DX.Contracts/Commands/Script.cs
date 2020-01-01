using DX.Contracts;
using DX.Cqrs.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DX.Cqrs.Commands {
    [Contract]
    public class Script {
        public string? Name { get; }

        public ScriptCommand[] Commands { get;  }

        public Script(string? name, ScriptCommand[] commands) {
            Name = name;
            Commands = Check.NotNull(commands, nameof(commands));
        }
    }

    public class ScriptBuilder : IBuilds<Script> {
        public string? Name { get; }

        public ScriptCommandCollection Commands { get; } = new ScriptCommandCollection();

        public ScriptBuilder(Script? source = null) {
            if (source != null) {
                Name = source.Name;
                foreach (ScriptCommand c in source.Commands)
                    Commands.Add(c);
            }
        }

        public Script Build()
            => new Script(Name, Commands.ToArray());

        public static implicit operator Script(ScriptBuilder builder)
            => builder.Build();
    }
}
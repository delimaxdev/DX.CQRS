using DX.Contracts;
using DX.Cqrs.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Cqrs.Maintenance {
    public abstract class MaintenanceScript {
        public ID ScriptID { get; }

        public string Name { get; }

        protected MaintenanceScript(string scriptID, string? name = null) {
            ScriptID = ID.Parse(scriptID);
            Name = name ?? GetType().Name;
        }

        public abstract ScriptCommandCollection Build();
    }
}
using DX.Contracts;
using DX.Cqrs.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DX.Cqrs.Commands {
    public class ScriptCommandCollection : Collection<ScriptCommand> {
        public void Add(ID commandID, ID? target, ICommandMessage message) {
            Add(new ScriptCommand(commandID, target, message));
        }

        public void Add(string commandID, ID? target, ICommandMessage message) {
            Add(ID.Parse(commandID), target, message);
        }

        public void Add(string commandID, ID? target, IBuilds<ICommandMessage> builder) {
            Add(ID.Parse(commandID), target, builder.Build());
        }
        
        public void Add<T>(string commandID, Ref<T> target, ICommandMessage message) where T : IHasID<ID> {
            Add(ID.Parse(commandID), ID.FromRef(target), message);
        }

        public void Add<T>(string commandID, Ref<T> target, IBuilds<ICommandMessage> builder) where T : IHasID<ID> {
            Add(ID.Parse(commandID), ID.FromRef(target), builder.Build());
        }
    }
}
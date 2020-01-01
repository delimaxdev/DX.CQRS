using DX.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Cqrs.Commands {
    [Contract]
    [Builder(typeof(ScriptCommandBuilder))]
    public class ScriptCommand {
        public ID ID { get; }

        public ID? Target { get; }

        public ICommandMessage Message { get; }

        public ScriptCommand(ID commandID, ID? targetID, ICommandMessage message) {
            ID = Check.NotNull(commandID, nameof(commandID));
            Target = targetID;
            Message = Check.NotNull(message, nameof(message));
        }
    }

    public class ScriptCommandBuilder : IBuilds<ScriptCommand> {
        public ID? ID { get; set; }

        public ID? Target { get; set; }

        public ICommandMessage? Message { get; set; }

        public ScriptCommandBuilder(ScriptCommand? source = null) {
            if (source != null) {
                ID = source.ID;
                Target = source.Target;
                Message = source.Message;
            }
        }

        public ScriptCommand Build() {
            return new ScriptCommand(ID.NotNull(), Target, Message.NotNull());
        }
    }
}
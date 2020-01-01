using DX.Contracts;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Messaging;
using System;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;

namespace DX.Cqrs.Commands {
    public abstract class CommandHandler : Receivable, ICommandHandler {
        protected void HandleCommand<TMessage>(Func<TMessage, IContext, Task> handler) where TMessage : ICommandMessage {
            Register<HandleCommand, Task>(hc => {
                if (hc.Command.Message is TMessage m) {
                    return handler(m, hc.Context);
                }

                return None<Task>.Value;
            });
        }

        protected void HandleCommand<TMessage>(Func<TMessage, ID?, IContext, Task> handler) where TMessage : ICommandMessage {
            Register<HandleCommand, Task>(hc => {
                if (hc.Command.Message is TMessage m) {
                    return handler(m, hc.Command.Target, hc.Context);
                }

                return None<Task>.Value;
            });
        }
    }
}
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Messaging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Web.Controllers {
    public class CommandsControllerBase : ControllerBase {
        private readonly IContext _context;
        private readonly ICommandProcessor _processor;

        public CommandsControllerBase(ICommandProcessor processor, IContext context) {
            _processor = processor;
            _context = context;
        }

        [HttpPost]
        public async Task Post([FromBody]Command command) {
            _context.Set(new RequestTimeContext(DateTime.Now), true);

            QueueCommandResult result = await _processor
                .Send<QueueCommand, Task<QueueCommandResult>>(new QueueCommand(command, _context));

            switch (result.Type) {
                case QueueCommandResultType.Rejected:
                    // TODO: Handle queue full scenario
                    throw new InvalidOperationException("Queue failed");
                case QueueCommandResultType.AlreadyExecuted:
                    return;
                case QueueCommandResultType.SuccessfullyQueued:
                    // HACK: Make processing essentially synchronous for the moment...
                    await result.Completion;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}

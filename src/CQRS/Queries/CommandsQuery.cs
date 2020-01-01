using DX.Contracts.Cqrs.Domain;
using DX.Contracts.Cqrs.Queries;
using DX.Cqrs.Common;
using DX.Cqrs.EventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DX.Cqrs.Queries {
    public class CommandsQuery : CollectionQuery<CommandRM>, IQuery<GetAllCommands, IReadOnlyCollection<CommandRM>> {
        public CommandsQuery(IEventStore store) : base(store) {
            On<IServerCommand.Created>((s, e, id) => s.Add(new CommandRM(
                id, 
                e.Message.GetType().FullName!,
                e.Target,
                CommandState.Created
            )));
            On<IServerCommand.Queued>((s, e, id) => {
                s.Update(id, rm => {
                    rm.RequestTime = e.Metadata.RequestTime;
                    rm.State = CommandState.Queued;
                    rm.Parent = e.Metadata.Parent;

                    if (rm.Parent != null) {
                        s.Update(rm.Parent, p => {
                            p.Commands.Add(rm);
                        });
                    }
                });
            });
            On<IServerCommand.Started>((s, e, id) => s.Update(id, rm => { 
                rm.StartTime = e.Timestamp;
                rm.State = CommandState.Started;
            }));
            On<IServerCommand.Succeeded>((s, e, id) => s.Update(id, rm => {
                rm.Duration = e.Timestamp - rm.StartTime;
                rm.State = CommandState.Succeeded;
            }));
            On<IServerCommand.Failed>((s, e, id) => s.Update(id, rm => {
                rm.Duration = e.Timestamp - rm.StartTime;
                rm.State = CommandState.Failed;
                rm.FailureMessage = e.Message;
                rm.ExceptionType = e.ExceptionType;
                rm.ExceptionStacktrace = e.ExceptionStacktrace;
            }));
        }

        public async Task<IReadOnlyCollection<CommandRM>> Run(GetAllCommands criteria, IContext context) {
            var s = (await Run(new CollectionQueryState<CommandRM>(), context)).Result();
            return s
                .Where(x => x.Parent == null)
                .ToArray();
        }
    }
}
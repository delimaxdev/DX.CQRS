using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.Common;

namespace DX.Cqrs.Commands
{
    public class DefaultCommandMetadataProvider : ICommandMetadataProvider {
        public CommandMetadata Provide(IContext context) {
            Ref<IServerCommand>? parent = context.TryGet(out CommandContext c) ?
                c.ExecutingCommand.ToRef<IServerCommand>() :
                null;

            return new CommandMetadata(
                context.Get<RequestTimeContext>().RequestTime,
                parent
            );
        }
    }
}
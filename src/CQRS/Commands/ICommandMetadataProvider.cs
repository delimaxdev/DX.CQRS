using DX.Cqrs.Common;

namespace DX.Cqrs.Commands
{
    public interface ICommandMetadataProvider {
        CommandMetadata Provide(IContext context);
    }
}
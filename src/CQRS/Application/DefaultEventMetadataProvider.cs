using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Cqrs.Domain;

namespace DX.Cqrs.Application
{
    public class DefaultEventMetadataProvider : IEventMetadataProvider {
        private readonly IContext _context;

        public DefaultEventMetadataProvider(IContext context) {
            _context = Check.NotNull(context, nameof(context));
        }

        public object Provide() {
            return new DefaultEventMetadata(
                _context.Get<TimestampContext>().Timestamp,
                _context.Get<CommandContext>().ExecutingCommand
            );
        }
    }
}
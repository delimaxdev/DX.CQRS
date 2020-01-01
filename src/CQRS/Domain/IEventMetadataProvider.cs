namespace DX.Cqrs.Domain
{
    public interface IEventMetadataProvider {
        object Provide();
    }
}
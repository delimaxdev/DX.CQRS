namespace DX.Cqrs.Domain
{
    public interface ICauses<out TEvent> {
        TEvent BuildEvent();
    }
}

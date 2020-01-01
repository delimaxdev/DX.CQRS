namespace DX.Contracts {
    [Contract(IsPolymorphic = true)]
    public interface IEvent {
    }

    public interface IEvent<TParent> : IEvent {
    }
}
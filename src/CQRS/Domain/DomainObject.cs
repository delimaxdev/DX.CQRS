namespace DX.Cqrs.Domain {
    public class DomainObject {
        protected Messenger M { get; }

        public DomainObject(Messenger messenger)
            => M = Check.NotNull(messenger, nameof(messenger));
    }
}

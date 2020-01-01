namespace DX.Messaging {
    public class Receivable : MessageHandler, IReceivable {
        private ILinkedListNode _tail;

        public Receivable() {
            _tail = this;
        }

        protected void AddHandler(MessageHandler handler) {
            _tail.InsertAfter(handler);
            _tail = handler;
        }
    }
}

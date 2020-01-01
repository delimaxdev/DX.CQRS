namespace DX.Cqrs.EventStore.Mongo {
     internal class EventIDGenerator {
        private EventID _currentID;
        private bool _isInitialInvoke = true;

        public EventIDGenerator(BatchID batchID)
            => _currentID = EventID.GetFirst(batchID);

        public EventID Next() {
            if (_isInitialInvoke) {
                _isInitialInvoke = false;
                return _currentID;
            }

            _currentID = _currentID.GetNext();
            return _currentID;
        }
    }
}

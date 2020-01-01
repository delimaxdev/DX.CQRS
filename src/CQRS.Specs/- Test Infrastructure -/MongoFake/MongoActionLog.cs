using System;
using System.Collections.Generic;

namespace Mongo
{
    internal class MongoActionLog : MongoActionFluentInterface<MongoActionLog> {
        private List<MongoAction> _actions = new List<MongoAction>();

        public IEnumerable<MongoAction> Actions => _actions;

        public MongoActionLog Transaction(Action<MongoActionLog> configAction) {
            var log = new MongoActionLog();
            configAction(log);
            return OnCreate(new MongoActions.Transaction(log));
        }

        public void Add(MongoAction action)
            => _actions.Add(action);

        public void Clear()
            => _actions.Clear();

        protected override MongoActionLog OnCreate(MongoAction action) {
            _actions.Add(action);
            return this;
        }
    }
}

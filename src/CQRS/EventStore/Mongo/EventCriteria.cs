using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DX.Cqrs.EventStore.Mongo
{
    internal class EventCriteriaBuilder : IEventCriteriaBuilder {
        private readonly List<object> _streamIDs = new List<object>();
        private readonly List<Type> _eventTypes = new List<Type>();

        public void Stream(object streamID)
            => _streamIDs.Add(Check.NotNull(streamID, nameof(streamID)));

        public void Type<TEvent>()
            => _eventTypes.Add(typeof(TEvent));

        public EventCriteria BuildCriteria(ITypeNameResolver typeNameResolver) {
            var filters = new List<FilterDefinition<RecordedEvent>>();

            IEnumerable<string> eventNames = _eventTypes
                .Select(t => typeNameResolver.GetTypeName(t))
                .ToArray();

            if (_streamIDs.Any())
                filters.Add(Builders<RecordedEvent>.Filter.In("Event._t", eventNames));

            if (_eventTypes.Any())
                filters.Add(Builders<RecordedEvent>.Filter.In(x => x.StreamID, _streamIDs));

            FilterDefinition<RecordedEvent> filter = filters.Count switch {
                0 => Builders<RecordedEvent>.Filter.Empty,
                1 => filters.Single(),
                _ => Builders<RecordedEvent>.Filter.And(filters)
            };

            return new EventCriteria(filter);
        }
    }

    internal class EventCriteria : IEventCriteria {
        public static readonly EventCriteria Empty = new EventCriteria(Builders<RecordedEvent>.Filter.Empty);

        public FilterDefinition<RecordedEvent> Filter { get; }

        public EventCriteria(FilterDefinition<RecordedEvent> filter)
            => Filter = filter;

        public override string ToString()
            => Filter.ToString();
    }
}

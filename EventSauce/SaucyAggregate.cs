using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EventSauce.Tests")]

namespace EventSauce
{
    public abstract class SaucyAggregate
    {
        private readonly ICollection<SaucyEvent> _uncommittedEvents = new LinkedList<SaucyEvent>();

        internal const long NewAggregateVersion = -1;

        public SaucyAggregateId? Id { get; private set; }

        public long Version { get; private set; } = NewAggregateVersion;

        internal void ApplyEvent(SaucyEvent saucyEvent)
        {
            // Event was already applied
            if (_uncommittedEvents.Any(x => x.EventId == saucyEvent.EventId))
            {
                return;
            }

            if (Id == null)
            {
                Id = saucyEvent.AggregateId;
            }

            // Apparently this is faster
            // than switch cases
            ((dynamic)this).Apply((dynamic)saucyEvent);

            Version = saucyEvent.AggregateVersion;
        }

        internal void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        internal IEnumerable<SaucyEvent> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsEnumerable();
        }

        protected void IssueEvent(SaucyEvent saucyEvent)
        {
            var eventWithAggregate = HydrateEvent(saucyEvent);

            ApplyEvent(eventWithAggregate);

            _uncommittedEvents.Add(eventWithAggregate);
        }

        private SaucyEvent HydrateEvent(SaucyEvent saucyEvent)
        {
            var aggregateId = Id ?? saucyEvent.AggregateId;

            if (aggregateId == null || string.IsNullOrEmpty(aggregateId.Kind) || aggregateId.Id == Guid.Empty)
            {
                throw new EventSauceException($"Aggregate ID {aggregateId} is not valid");
            }

            if (saucyEvent.EventId == Guid.Empty)
            {
                throw new EventSauceException($"Event ID {saucyEvent.EventId} is not valid");
            }

            return saucyEvent with
            {
                AggregateId = aggregateId,
                AggregateVersion = Version + 1
            };
        }
    }

    public abstract record SaucyAggregateId
    {
        public string Kind { get; init; } = string.Empty;

        public Guid Id { get; init; }

        public override string ToString()
        {
            return $"[{Kind}|{Id}]";
        }
    }

    public abstract record SaucyEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public DateTime Created { get; init; } = DateTime.UtcNow;

        public SaucyAggregateId? AggregateId { get; init; }

        public long AggregateVersion { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("EventSauce.Tests")]

namespace EventSauce
{
    public abstract class SaucyAggregate<TAggregateId> where TAggregateId : SaucyAggregateId
    {
        private readonly ICollection<SaucyEvent<TAggregateId>> _uncommittedEvents = new LinkedList<SaucyEvent<TAggregateId>>();

        internal const long NewAggregateVersion = -1;

        public TAggregateId? Id { get; private set; }

        public long Version { get; private set; } = NewAggregateVersion;

        internal void ApplyEvent(SaucyEvent<TAggregateId> saucyEvent)
        {
            // Event was already applied
            if (_uncommittedEvents.Any(x => x.Id == saucyEvent.Id))
            {
                return;
            }

            Id ??= saucyEvent.AggregateId;

            // Apparently this is faster
            // than switch cases
            ((dynamic)this).Apply((dynamic)saucyEvent);

            Version = saucyEvent.AggregateVersion;
        }

        internal void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        internal IEnumerable<SaucyEvent<TAggregateId>> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsEnumerable();
        }

        protected void IssueEvent(SaucyEvent<TAggregateId> saucyEvent)
        {
            var eventWithAggregate = HydrateEvent(saucyEvent);

            ApplyEvent(eventWithAggregate);

            _uncommittedEvents.Add(eventWithAggregate);
        }

        private SaucyEvent<TAggregateId> HydrateEvent(SaucyEvent<TAggregateId> saucyEvent)
        {
            var aggregateId = Id ?? saucyEvent.AggregateId;

            if (aggregateId == null || string.IsNullOrEmpty(aggregateId.IdType) || aggregateId.Id == Guid.Empty)
            {
                throw new EventSauceException($"Aggregate ID {aggregateId} is not valid");
            }

            return saucyEvent with
            {
                AggregateId = aggregateId,
                AggregateVersion = Version + 1
            };
        }
    }

    public abstract record SaucyAggregateId(Guid Id)
    {
        public string IdType => GetType().Name;

        public override string ToString()
        {
            return $"[{IdType}|{Id}]";
        }
    }

    public abstract record SaucyEvent<TAggregateId> where TAggregateId : SaucyAggregateId
    {
        [JsonIgnore] private string IdType => GetType().Name;

        [JsonIgnore] public Guid Id { get; init; } = Guid.NewGuid();

        [JsonIgnore] public DateTime Created { get; init; } = DateTime.UtcNow;

        [JsonIgnore] public TAggregateId? AggregateId { get; init; }

        [JsonIgnore] public long AggregateVersion { get; init; }

        public override string ToString()
        {
            return $"[{IdType}|{Id}|Aggregate: {AggregateId}|Version: {AggregateVersion}]";
        }
    }
}

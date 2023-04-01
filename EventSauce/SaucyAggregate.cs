using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("EventSauce.Tests")]

namespace EventSauce
{
    public abstract class SaucyAggregate<TAggregateId>
    {
        private readonly ICollection<SaucyEvent<TAggregateId>> _uncommittedEvents = new LinkedList<SaucyEvent<TAggregateId>>();

        internal const long NewAggregateVersion = -1;

        public TAggregateId? Id { get; protected internal set; }

        public long Version { get; private set; } = NewAggregateVersion;

        internal void ApplyEvent(SaucyEvent<TAggregateId> saucyEvent)
        {
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
            saucyEvent = saucyEvent with
            {
                AggregateId = Id,
                AggregateVersion = Version + 1
            };

            // Event was already applied
            if (_uncommittedEvents.Any(x => x == saucyEvent))
            {
                return;
            }

            ApplyEvent(saucyEvent);

            _uncommittedEvents.Add(saucyEvent);
        }
    }

    public abstract record SaucyEvent<TAggregateId>
    {
        [JsonIgnore] private string IdType => GetType().Name;

        [JsonIgnore] public DateTime Created { get; init; } = DateTime.UtcNow;

        [JsonIgnore] public TAggregateId? AggregateId { get; init; }

        [JsonIgnore] public long AggregateVersion { get; init; }

        public override string ToString()
        {
            return $"[{IdType}|Aggregate: {AggregateId}|Version: {AggregateVersion}]";
        }
    }
}

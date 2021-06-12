using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSauce
{
    public interface ISauceStore : IDisposable
    {
        Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id);

        Task AppendEvent(SaucyEvent sourceEvent, SaucyAggregateId? performedBy);
    }

    public interface ISaucyBus
    {
        Task Publish(SaucyEvent saucyEvent);
    }

    public class StubbedSauceBus : ISaucyBus
    {
        public Task Publish(SaucyEvent saucyEvent)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class InMemorySauceStore : ISauceStore
    {
        private static readonly List<SaucyEvent> Sauces = new ();

        public Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            return Task.FromResult(Sauces.Where(x => x.AggregateId == id));
        }

        public Task AppendEvent(SaucyEvent sourceEvent, SaucyAggregateId? performedBy)
        {
            Sauces.Add(sourceEvent);

            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
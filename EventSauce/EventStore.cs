using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSauce
{
    public interface ISauceStore
    {
        Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id);

        Task AppendEvent(SaucyEvent sourceEvent);
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

    public class InMemorySauceStore : ISauceStore
    {
        private static readonly List<SaucyEvent> Sauces = new ();

        public Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            return Task.FromResult(Sauces.Where(x => x.AggregateId == id));
        }

        public Task AppendEvent(SaucyEvent sourceEvent)
        {
            Sauces.Add(sourceEvent);

            return Task.CompletedTask;
        }
    }
}
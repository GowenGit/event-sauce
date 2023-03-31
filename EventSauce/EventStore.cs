using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSauce
{
    public interface ISauceStore : IDisposable
    {
        Task<IEnumerable<SaucyEvent<TAggregateId>>> ReadEvents<TAggregateId>(TAggregateId id) where TAggregateId : SaucyAggregateId;

        Task AppendEvent<TAggregateId>(SaucyEvent<TAggregateId> sourceEvent, SaucyAggregateId? performedBy) where TAggregateId : SaucyAggregateId;
    }

    public interface ISaucyBus
    {
        Task Publish<TAggregateId>(SaucyEvent<TAggregateId> saucyEvent) where TAggregateId : SaucyAggregateId;
    }

    public class StubbedSauceBus : ISaucyBus
    {
        public Task Publish<TAggregateId>(SaucyEvent<TAggregateId> saucyEvent) where TAggregateId : SaucyAggregateId
        {
            return Task.CompletedTask;
        }
    }

    public sealed class InMemorySauceStore : ISauceStore
    {
        private static readonly Dictionary<string, List<object>> Store = new();

        public void Dispose()
        {
        }

        public Task<IEnumerable<SaucyEvent<TAggregateId>>> ReadEvents<TAggregateId>(TAggregateId id) where TAggregateId : SaucyAggregateId
        {
            var type = typeof(TAggregateId);

            if (!Store.ContainsKey(type.FullName!))
            {
                return Task.FromResult((IEnumerable<SaucyEvent<TAggregateId>>)new List<SaucyEvent<TAggregateId>>());
            }

            return Task.FromResult(Store[type.FullName!].Cast<SaucyEvent<TAggregateId>>().Where(x => x.AggregateId == id));
        }

        public Task AppendEvent<TAggregateId>(SaucyEvent<TAggregateId> sourceEvent, SaucyAggregateId? performedBy) where TAggregateId : SaucyAggregateId
        {
            var type = typeof(TAggregateId);

            if (!Store.ContainsKey(type.FullName!))
            {
                Store[type.FullName!] = new List<object>();
            }

            Store[type.FullName!].Add(sourceEvent);

            return Task.CompletedTask;
        }
    }
}

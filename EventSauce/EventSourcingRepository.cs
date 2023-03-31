using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EventSauce
{
    public interface ISaucyRepository
    {
        Task<TAggregate?> GetById<TAggregate, TAggregateId>(TAggregateId id, CancellationToken cancellationToken = default)
            where TAggregate : SaucyAggregate<TAggregateId>
            where TAggregateId : SaucyAggregateId;

        Task Save<TAggregate, TAggregateId>(TAggregate aggregate, SaucyAggregateId? performedBy = null, CancellationToken cancellationToken = default)
            where TAggregate : SaucyAggregate<TAggregateId>
            where TAggregateId : SaucyAggregateId;
    }

    public class SaucyRepository : ISaucyRepository
    {
        private readonly ISauceStore _sauceStore;
        private readonly ISaucyBus _eventBus;

        public SaucyRepository(
            ISauceStore sauceStore,
            ISaucyBus saucyBus)
        {
            _sauceStore = sauceStore;
            _eventBus = saucyBus;
        }

        public async Task<TAggregate?> GetById<TAggregate, TAggregateId>(TAggregateId id, CancellationToken cancellationToken = default)
            where TAggregate : SaucyAggregate<TAggregateId>
            where TAggregateId : SaucyAggregateId
        {
            try
            {
                var aggregate = CreateEmptyAggregate<TAggregate>();

                foreach (var domainEvent in await _sauceStore.ReadEvents(id))
                {
                    aggregate.ApplyEvent(domainEvent);
                }

                return aggregate.Version == SaucyAggregate<TAggregateId>.NewAggregateVersion ? null : aggregate;
            }
            catch (EventSauceAggregateNotFoundException)
            {
                return null;
            }
            catch (EventSauceCommunicationException ex)
            {
                throw new EventSauceRepositoryException("Unable to access persistence layer", ex);
            }
        }

        public async Task Save<TAggregate, TAggregateId>(TAggregate aggregate, SaucyAggregateId? performedBy = null, CancellationToken cancellationToken = default)
            where TAggregate : SaucyAggregate<TAggregateId>
            where TAggregateId : SaucyAggregateId
        {
            try
            {
                foreach (var domainEvent in aggregate.GetUncommittedEvents())
                {
                    await _sauceStore.AppendEvent(domainEvent, performedBy);
                    await _eventBus.Publish(domainEvent);
                }

                aggregate.ClearUncommittedEvents();
            }
            catch (EventSauceCommunicationException ex)
            {
                throw new EventSauceRepositoryException("Unable to access persistence layer", ex);
            }
        }

        private static TAggregate CreateEmptyAggregate<TAggregate>()
        {
            const BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var constructor = typeof(TAggregate)
                .GetConstructor(bindingAttr, null, Array.Empty<Type>(), Array.Empty<ParameterModifier>());

            if (constructor == null)
            {
                throw new ArgumentNullException($"{typeof(TAggregate)} has no default private constructor");
            }

            return (TAggregate)constructor.Invoke(Array.Empty<object>());
        }
    }
}

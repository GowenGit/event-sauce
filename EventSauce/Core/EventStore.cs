using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventSauce.Core
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
}
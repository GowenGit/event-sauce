using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EventSauce.Core;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1623

[assembly: InternalsVisibleTo("EventSauce.Tests")]

namespace EventSauce
{
    public static class ConfigureEventSauce
    {
        public static void AddEventSauce(this IServiceCollection services, Action<EventSauceOptions>? options = null)
        {
            var settings = new EventSauceOptions();

            options?.Invoke(settings);

            services.AddTransient<ISaucyRepository, SaucyRepository>();
            services.AddTransient(settings.Store);
            services.AddTransient(settings.Bus);
        }
    }

    public class EventSauceOptions
    {
        /// <summary>
        /// Assemblies to scan for aggregates and saucy events.
        /// </summary>
        public Assembly[] Assemblies { get; set; } = Array.Empty<Assembly>();

        public Func<IServiceProvider, ISauceStore> Store { get; set; } = x => new InMemorySauceStore();

        public Func<IServiceProvider, ISaucyBus> Bus { get; set; } = x => new IncompetentBus();
    }

    internal class IncompetentBus : ISaucyBus
    {
        public Task Publish(SaucyEvent saucyEvent)
        {
            return Task.CompletedTask;
        }
    }

    internal class InMemorySauceStore : ISauceStore
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

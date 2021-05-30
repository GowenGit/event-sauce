using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

#pragma warning disable SA1623

namespace EventSauce.Extensions.Microsoft.DependencyInjection
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

        public Func<IServiceProvider, ISaucyBus> Bus { get; set; } = x => new StubbedSauceBus();
    }
}

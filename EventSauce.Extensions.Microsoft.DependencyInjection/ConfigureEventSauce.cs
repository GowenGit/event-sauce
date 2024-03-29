﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace EventSauce.Extensions.Microsoft.DependencyInjection
{
    public static class ConfigureEventSauce
    {
        public static void AddEventSauce(this IServiceCollection services, Action<EventSauceOptions>? options = null)
        {
            var settings = new EventSauceOptions();

            options?.Invoke(settings);

            services.AddTransient<ISaucyRepository, SaucyRepository>();

            services.Add(new ServiceDescriptor(typeof(ISauceStore), settings.Store, settings.StoreLifetime));
            services.Add(new ServiceDescriptor(typeof(ISaucyBus), settings.Bus, settings.BusLifetime));
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

        public ServiceLifetime BusLifetime { get; set; } = ServiceLifetime.Transient;

        public ServiceLifetime StoreLifetime { get; set; } = ServiceLifetime.Transient;
    }
}

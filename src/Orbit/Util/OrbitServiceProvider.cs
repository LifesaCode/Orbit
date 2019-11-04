﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbit.Data;
using System;
using System.Linq;
using System.Reflection;
using Orbit.Components;
using System.Resources;
using Orbit.Models;

namespace Orbit.Util
{
    public class OrbitServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _provider;

        private OrbitServiceProvider()
        {
            this._provider = this.Build();
        }

        private static readonly Lazy<OrbitServiceProvider> _instance = new Lazy<OrbitServiceProvider>(() => new OrbitServiceProvider());

        public static IServiceProvider Instance => _instance.Value;

        public static event EventHandler<IServiceCollection>? OnRegisteringServices;

        private IServiceProvider Build()
        {
            var services = new ServiceCollection();
            this.RegisterServices(services);
            OnRegisteringServices?.Invoke(this, services);
            ServiceProvider prov = services.BuildServiceProvider();
            EventMonitor.Instance.Start();
            return prov;
        }

        private void RegisterServices(IServiceCollection services)
        {
            /* Registering "open" types allows the provider to map the requested type parameter appropriately,
             * assuming it exists in the database
             *
             * services.AddScoped(typeof(IMonitoredComponent<>), typeof(MonitoredComponent<>));
             * services.AddScoped(typeof(MonitoredComponent<>));
             */

            // This is how you can register a concrete implementation
            // With only one valid report type (BatteryReport), the following is equivalent to the above commented out code
            services.AddScoped<IMonitoredComponent<BatteryReport>, BatteryComponent>();

            // Another way could be just directly registering the concrete class
            //services.AddScoped<BatteryComponent>();

            // Allow the singleton instance of the event monitor to be retrieved by the service provider if desired
            services.AddSingleton(_ => EventMonitor.Instance);

            //TODO: Replace the following with actual database implementation when ready
            services.AddDbContext<OrbitDbContext>(o =>
            {
                o.UseInMemoryDatabase("OrbitDb");
            });
        }
        
        object? IServiceProvider.GetService(Type serviceType) => this._provider.GetService(serviceType);
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Orbit.Data;
using Orbit.Util;

namespace Orbit.Components
{
    public class MonitoredComponent<T> : IMonitoredComponent<T>
        where T : class, IModel
    {
        private readonly Lazy<string> _componentName;

        public string ComponentName => _componentName.Value;

        protected OrbitDbContext Database { get; }

        public MonitoredComponent(OrbitDbContext db)
        {
            this.Database = db;
            _componentName = new Lazy<string>(() => this.Database.Set<T>().AsNoTracking().Select(r => r.ComponentName).First());
        }

        public virtual async ValueTask<IReadOnlyCollection<T>> GetReportsAsync(int? maxResults = 10, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = this.Database.Set<T>();

            if (maxResults != null)
                query = query.Take(maxResults.Value);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously returns the latest available report of type <typeparamref name="T"/> available.
        /// </summary>
        public async ValueTask<T?> GetLatestReportAsync()
        {
            var set = this.Database.Set<T>();
            T? val = await set.AsNoTracking().LastOrDefaultAsync().ConfigureAwait(false);

            return val;
        }

        #region Explicit implementation for simplification of EventMonitor

        async ValueTask<IModel?> IMonitoredComponent.GetLatestReportAsync() => await this.GetLatestReportAsync().ConfigureAwait(false);

        async ValueTask<IReadOnlyCollection<IModel>> IMonitoredComponent.GetReportsAsync(int? maxResults, CancellationToken cancellationToken)
        {
            return await this.GetReportsAsync(maxResults, cancellationToken).ConfigureAwait(false);
        }

        #endregion Explicit implementation for simplification of EventMonitor
    }
}
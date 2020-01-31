using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Orbit.Data;
using Orbit.Models;

namespace Orbit.Util
{
    public sealed class DataGenerator : IDataGenerator
    {
        private static readonly Lazy<IDataGenerator> _instance = new Lazy<IDataGenerator>(() => new DataGenerator());
        public static IDataGenerator Instance => _instance.Value;

        private DataGenerator()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => this._cancellationTokenSource.Cancel();
        }

        private Thread? _eventThread;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private IServiceProvider ServiceProvider => OrbitServiceProvider.Instance;

        public void Start()
        {
            if (_eventThread == null)
            {
                _eventThread = new Thread(SimulateDataGeneration);
                _eventThread.Start();
            }
        }

        public void Stop()
        {
            if (!this._cancellationTokenSource.IsCancellationRequested)
            {
                this._cancellationTokenSource.Cancel();
            }
        }

        public event EventHandler? Started;

        public event EventHandler? Stopped;

        /// <summary>
        /// This method simulates generating real-world data and inserting it into the database.
        /// </summary>
        /// <returns> </returns>
        private void SimulateDataGeneration()
        {
            CancellationToken token = _cancellationTokenSource.Token;

            this.Started?.Invoke(this, EventArgs.Empty);
            bool isFirst = true;
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));

                if (token.IsCancellationRequested)
                {
                    break;
                }

                using var scope = ServiceProvider.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<OrbitDbContext>();

                if (isFirst)
                {
                    db.InsertSeedData();
                    isFirst = false;
                    db.SaveChanges();
                    continue;
                }

                WasteWaterStorageTankData wasteTank = db.WasteWaterStorageTankData.Last();
                UrineSystemData urineSystem = db.UrineProcessorData.Last();
                WaterProcessorData waterProcessor = db.WaterProcessorData.Last();

                urineSystem.ProcessData(wasteTank.Level);
                wasteTank.ProcessData(urineSystem.Status, waterProcessor.SystemStatus);
                waterProcessor.ProcessData(wasteTank.Level);

                var nextUrineSystem = new UrineSystemData(urineSystem);
                var nextWasteTank = new WasteWaterStorageTankData {
                    TankId = "Main",
                    Level = wasteTank.Level,
                };

                var nextWaterProcessor = new WaterProcessorData(waterProcessor);

                db.UrineProcessorData.Add(nextUrineSystem);
                db.WasteWaterStorageTankData.Add(nextWasteTank);
                db.WaterProcessorData.Add(nextWaterProcessor);
                db.SaveChanges();
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);
        }
    }
}

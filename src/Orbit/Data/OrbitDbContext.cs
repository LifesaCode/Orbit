#nullable disable warnings

using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Orbit.Models;

namespace Orbit.Data
{
    public class OrbitDbContext : DbContext
    {
        public OrbitDbContext(DbContextOptions<OrbitDbContext> options) : base(options)
        {
        }

        public DbSet<UrineSystemData> UrineProcessorData { get; set; }

        public DbSet<WaterProcessorData> WaterProcessorData { get; set; }

        public DbSet<WasteWaterStorageTankData> WasteWaterStorageTankData { get; set; }

        public DbSet<AtmosphereData> AtmosphereData { get; set; }

        public DbSet<OxygenGenerator> OxygenGeneratorData { get; set; }

        public DbSet<CarbonDioxideRemediation> CarbonDioxideRemoverData { get; set; }

        public DbSet<PowerSystemData> PowerSystemData { get; set; }


        public DbSet<InternalCoolantLoopData> InternalCoolantLoopData { get; set; }

        public DbSet<ExternalCoolantLoopData> ExternalCoolantLoopData { get; set; }

        public DbSet<WaterGeneratorData> WaterGeneratorData { get; set; }

        public void InsertSeedData()
        {
            //TODO: Use NSubstitute for generating random seed data
            this.UrineProcessorData.Add(new UrineSystemData {
                BrineTankLevel = 5,
                DistillerSpeed = 20,
                DistillerTemp = 20,
                DistillerOn = false,
                SupplyPumpOn = false,
                PurgePumpOn = false,
                SystemStatus = SystemStatus.Standby,
                UrineTankLevel = 40,
            }); ;

            this.WasteWaterStorageTankData.Add(new WasteWaterStorageTankData {
                TankId = "Main",
                Level = 30,
            });

            this.WaterProcessorData.Add(new WaterProcessorData {
                FiltersOk = true,
                PostHeaterTemp= 20,
                ProductTankLevel = 80,
                PostReactorQualityOk = false,
                DiverterValvePosition = DiverterValvePositions.Reprocess,
                PumpOn = false,
                SystemStatus = SystemStatus.Standby,
            });
            this.SaveChanges();
        }

        private static void MapModelCommonProps<T>(EntityTypeBuilder<T> e) where T : class, IModel
        {
            e.Property<Guid>("Id").ValueGeneratedOnAdd();
            e.HasKey("Id");

            e.Property(p => p.ReportDateTime).ValueGeneratedOnAdd();
            e.HasAlternateKey(p => p.ReportDateTime);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
                throw new ArgumentNullException(nameof(modelBuilder));

            // Do this for all of these so we don't need to worry about defining the ids in code
            modelBuilder.Entity<UrineSystemData>(MapModelCommonProps);
            modelBuilder.Entity<WaterProcessorData>(MapModelCommonProps);
            modelBuilder.Entity<WasteWaterStorageTankData>(MapModelCommonProps);
            modelBuilder.Entity<AtmosphereData>(MapModelCommonProps);
            modelBuilder.Entity<CarbonDioxideRemediation>(MapModelCommonProps);
            modelBuilder.Entity<OxygenGenerator>(MapModelCommonProps);
            modelBuilder.Entity<PowerSystemData>(MapModelCommonProps);
            modelBuilder.Entity<ExternalCoolantLoopData>(MapModelCommonProps);
            modelBuilder.Entity<InternalCoolantLoopData>(MapModelCommonProps);
            modelBuilder.Entity<WaterGeneratorData>(MapModelCommonProps);
        }
    }
}
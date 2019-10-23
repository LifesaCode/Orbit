﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

using Orbit.Models;

using System;

namespace Orbit.Data
{
    public class OrbitDbContext : DbContext
    {
        public OrbitDbContext(DbContextOptions<OrbitDbContext> options) : base(options)
        {
        }

        public DbSet<Limit> Limits { get; set; }

        public DbSet<BatteryReport> BatteryReports { get; set; }

        public void InsertSeedData()
        {
            var lim = new Limit(Guid.NewGuid(), 400, 300, 50);

            Limits.Add(lim);
            BatteryReports.Add(new BatteryReport(DateTimeOffset.Now, 360) { Limit = lim });
            this.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Limit>(e =>
            {
                // Members like this one are "shadow" properties that are represented in the database, but we don't need
                // to define as visible class members
                e.Property<Guid>("Id").HasValueGenerator(typeof(GuidValueGenerator));
                e.HasKey("Id");
            });

            modelBuilder.Entity<ReportBase>(e =>
            {
                e.Property<Guid>("Id").ValueGeneratedOnAdd().HasValueGenerator(typeof(GuidValueGenerator));
                e.HasKey("Id");

                e.Property<Guid>("LimitId");
                e.HasOne(d => d.Limit).WithMany().HasForeignKey("LimitId");

                e.Property(p => p.ReportDateTime).ValueGeneratedOnAdd();
                e.HasAlternateKey(p => p.ReportDateTime);
            });

            modelBuilder.Entity<BatteryReport>(e =>
            {
                e.HasBaseType<ReportBase>();
            });
        }
    }
}
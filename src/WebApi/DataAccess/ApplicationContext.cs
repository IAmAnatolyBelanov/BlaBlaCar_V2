﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using WebApi.Models;

namespace WebApi.DataAccess
{
	public class ApplicationContext : DbContext
	{
		private readonly ILogger _logger = Log.ForContext<ApplicationContext>();

		public ApplicationContext(DbContextOptions options) : base(options)
		{
			ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		}

		public DbSet<Ride_Obsolete> Rides { get; set; }
		public DbSet<Leg_Obsolete> Legs { get; set; }
		public DbSet<CompositeLeg> CompositeLegs { get; set; }
		public DbSet<Reservation_Obsolete> Reservations { get; set; }
		public DbSet<Price> Prices { get; set; }

		public DbSet<User> Users { get; set; }
		public DbSet<PersonData> PersonDatas { get; set; }
		public DbSet<CloudApiResponseInfo> CloudApiResponseInfos { get; set; }
		public DbSet<DriverData> DriverDatas { get; set; }

		/// <summary>
		/// Не покажет создание триггеров и функций.
		/// </summary>
		public void Migrate()
		{
			var oldSql = Database.GenerateCreateScript();
			_logger.Information("Start migration from state {State}", oldSql);

			Database.Migrate();

			var newSql = Database.GenerateCreateScript();
			_logger.Information("Migration from state {OldState} is finished. New state is {NewState}", oldSql, newSql);
		}

		public async Task MigrateAsync(CancellationToken ct)
		{
			var oldSql = Database.GenerateCreateScript();
			_logger.Information("Start migration from state {State}", oldSql);

			await Database.MigrateAsync(ct);

			var newSql = Database.GenerateCreateScript();
			_logger.Information("Migration from state {OldState} is finished. New state is {NewState}", oldSql, newSql);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);

			optionsBuilder.LogTo(
				_logger.Debug,
				LogLevel.Debug,
				DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime);

			optionsBuilder.EnableSensitiveDataLogging();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasPostgresExtension("postgis");

			modelBuilder.Entity<Leg_Obsolete>(builder =>
			{
				builder.Property(x => x.From).HasColumnType("geography (point)");
				builder.Property(x => x.To).HasColumnType("geography (point)");
				builder.HasIndex(x => x.From).HasMethod("GIST");
				builder.HasIndex(x => x.To).HasMethod("GIST");

				builder.HasIndex(x => x.StartTime);
				builder.HasIndex(x => x.EndTime);

				builder.Property(x => x.Description).HasDefaultValue("empty");

				//builder.HasOne<Leg>().WithOne().HasForeignKey<Leg>(x => x.NextLegId);
				//builder.HasOne<Leg>().WithOne().HasForeignKey<Leg>(x => x.PreviousLegId);

				builder.Ignore(x => x.NextLeg);
				builder.Ignore(x => x.PreviousLeg);

				builder.HasIndex(x => x.NextLegId);
				builder.HasIndex(x => x.PreviousLegId);
			});

			modelBuilder.Entity<CompositeLeg>(builder =>
			{
				builder.HasKey(x => new { x.MasterLegId, x.SubLegId });

				// Эти 2 индекса не то чтобы очень нужны, но ef насильно создаёт 1 из них.
				// Победить ef я не смог, так что решил возглавить.
				builder.HasIndex(x => x.MasterLegId);
				builder.HasIndex(x => x.SubLegId);

				builder.HasOne(x => x.MasterLeg)
					.WithMany()
					.HasForeignKey(x => x.MasterLegId);

				builder.HasOne(x => x.SubLeg)
					.WithMany()
					.HasForeignKey(x => x.SubLegId);
			});

			modelBuilder.Entity<Reservation_Obsolete>(builder =>
			{
				builder.HasIndex(x => new { x.StartLegId, x.EndLegId, x.UserId })
					.IsUnique()
					.HasFilter($"\"{nameof(Reservation_Obsolete.IsActive)}\" IS TRUE")
					.HasDatabaseName(DbConstants.IndexNames.Reservation_UniqueIfActive);

				builder.HasOne(x => x.StartLeg)
					.WithMany()
					.HasForeignKey(x => x.StartLegId);
				builder.HasOne(x => x.EndLeg)
					.WithMany()
					.HasForeignKey(x => x.EndLegId);

				builder.HasIndex(x => x.UserId);
				builder.HasIndex(x => x.CreateDateTime);
			});

			modelBuilder.Entity<Price>(builder =>
			{
				builder.HasIndex(x => x.StartLegId);
				builder.HasIndex(x => x.EndLegId);

				builder.HasIndex(x => new { x.StartLegId, x.EndLegId })
					.IsUnique();
			});

			modelBuilder.Entity<User>(builder =>
			{
				builder.HasKey(x => x.Id);
			});

			modelBuilder.Entity<PersonData>(builder =>
			{
				builder.HasKey(x => x.Id);
				builder.HasIndex(x => x.Inn);
				builder.HasIndex(x => new { x.PassportSeries, x.PassportNumber })
					.IsUnique();

				builder.HasOne<User>()
					.WithMany()
					.HasForeignKey(x => x.UserId);
			});

			modelBuilder.Entity<CloudApiResponseInfo>(builder =>
			{
				builder.HasKey(x => x.Id);

				builder.Property(x => x.Response)
					.HasColumnType("jsonb");

				builder.Property(x => x.Created)
					.HasDefaultValueSql("now() at time zone 'utc'");

				builder.HasIndex(x => x.Created);
				builder.HasIndex(x => x.RequestBasePath);
			});

			modelBuilder.Entity<DriverData>(builder =>
			{
				builder.HasKey(x => x.Id);
				builder.HasIndex(x => new { x.LicenseSeries, x.LicenseNumber })
					.IsUnique();

				builder.HasOne<User>()
					.WithMany()
					.HasForeignKey(x => x.UserId);
			});
		}
	}

	public static class DbConstants
	{
		public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
			= typeof(DbConstants).GetAllStringConstantsRecursively()
				.ToDictionary(x => x.Value, x => (x.Holder, x.Name));

		public static class IndexNames
		{
			public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
				= typeof(IndexNames).GetAllStringConstantsRecursively()
					.ToDictionary(x => x.Value, x => (x.Holder, x.Name));

			public const string Reservation_UniqueIfActive
				= "Custom_DbIndexName_Reservations_UserId_StartLegId_EndLegId_UniqueIfActive";
		}

		public static class FunctionNames
		{
			public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
				= typeof(FunctionNames).GetAllStringConstantsRecursively()
					.ToDictionary(x => x.Value, x => (x.Holder, x.Name));
		}

		public static class FunctionErrors
		{
			public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
				= typeof(FunctionErrors).GetAllStringConstantsRecursively()
					.ToDictionary(x => x.Value, x => (x.Holder, x.Name));
		}

		public static class TriggerNames
		{
			public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
				= typeof(TriggerNames).GetAllStringConstantsRecursively()
				.ToDictionary(x => x.Value, x => (x.Holder, x.Name));
		}
	}
}

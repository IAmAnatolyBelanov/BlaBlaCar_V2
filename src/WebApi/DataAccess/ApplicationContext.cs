using Microsoft.EntityFrameworkCore;
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

		public DbSet<Ride> Rides { get; set; }
		public DbSet<Leg> Legs { get; set; }
		public DbSet<CompositeLeg> CompositeLegs { get; set; }
		public DbSet<Reservation> Reservations { get; set; }
		public DbSet<Price> Prices { get; set; }

		/// <summary>
		/// Не покажет создание триггеров и функций.
		/// </summary>
		public void Migrate()
		{
			var oldSql = Database.GenerateCreateScript();
			_logger.Information("Start migration from state {State}", oldSql);

			Database.Migrate();

			var newSql = Database.GenerateCreateScript();
			_logger.Information("Migration from state {OldState} is finnished. New state is {NewState}", oldSql, newSql);
		}

		public async Task MigrateAsync(CancellationToken ct)
		{
			var oldSql = Database.GenerateCreateScript();
			_logger.Information("Start migration from state {State}", oldSql);

			await Database.MigrateAsync(ct);

			var newSql = Database.GenerateCreateScript();
			_logger.Information("Migration from state {OldState} is finnished. New state is {NewState}", oldSql, newSql);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);

			optionsBuilder.LogTo(
				_logger.Debug,
				LogLevel.Debug,
				DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime);

			//optionsBuilder.EnableSensitiveDataLogging();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasPostgresExtension("postgis");

			modelBuilder.Entity<Leg>(builder =>
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

			modelBuilder.Entity<Reservation>(builder =>
			{
				builder.HasIndex(x => new { x.StartLegId, x.EndLegId, x.UserId })
					.IsUnique()
					.HasFilter($"\"{nameof(Reservation.IsActive)}\" IS TRUE")
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

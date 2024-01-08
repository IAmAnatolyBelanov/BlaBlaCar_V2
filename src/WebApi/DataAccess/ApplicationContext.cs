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

			optionsBuilder.EnableSensitiveDataLogging();
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
				builder.HasIndex(x => new { x.LegId, x.UserId })
					.IsUnique()
					.HasFilter($"\"{nameof(Reservation.IsActive)}\" IS TRUE")
					.HasDatabaseName(DbConstants.IndexNames.Reservation_UniqueIfActive);

				builder.HasOne(x => x.Leg)
					.WithMany()
					.HasForeignKey(x => x.LegId);
			});
		}
	}

	public static class DbConstants
	{
		public static readonly IReadOnlyDictionary<string, string> AllConstants
			= typeof(DbConstants).GetAllStringConstants().ToDictionary();

		public static readonly IReadOnlySet<string> AllConstantValues
			= AllConstants.Values.ToHashSet();

		public static class IndexNames
		{
			public static readonly IReadOnlyDictionary<string, string> AllConstants
				= typeof(IndexNames).GetAllStringConstants().ToDictionary();

			public static readonly IReadOnlySet<string> AllConstantValues
				= AllConstants.Values.ToHashSet();

			public const string Reservation_UniqueIfActive
				= "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive";
		}

		public static class FunctionNames
		{
			public static readonly IReadOnlyDictionary<string, string> AllConstants
				= typeof(FunctionNames).GetAllStringConstants().ToDictionary();

			public static readonly IReadOnlySet<string> AllConstantValues
				= AllConstants.Values.ToHashSet();

			public const string TriggerReservation_EnoughFreePlaces
				= "Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation";

			//public const string TriggerRide_EnoughFreePlaces
			//	= "Custom_DbFunctionName_RideHasEnoughFreePlaces_OnRide";
		}

		public static class FunctionErrors
		{
			public static readonly IReadOnlyDictionary<string, string> AllConstants
				= typeof(FunctionErrors).GetAllStringConstants().ToDictionary();

			public static readonly IReadOnlySet<string> AllConstantValues
				= AllConstants.Values.ToHashSet();

			public const string TriggerReservation_EnoughFreePlaces__NotEnougFreePlaces
				= "Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation_Error_NotEnoughFreePlaces";
			//public const string TriggerRide_EnoughFreePlaces__NotEnougFreePlaces
			//	= "Custom_DbFunctionName_RideHasEnoughFreePlaces_OnRide_Error_NotEnoughFreePlaces";
		}

		public static class TriggerNames
		{
			public static readonly IReadOnlyDictionary<string, string> AllConstants
				= typeof(TriggerNames).GetAllStringConstants().ToDictionary();

			public static readonly IReadOnlySet<string> AllConstantValues
				= AllConstants.Values.ToHashSet();

			public const string Reservation_EnoughFreePlaces
				= "Custom_DbTriggerName_RideHasEnoughFreePlaces_OnReservation";

			//public const string Ride_EnoughFreePlaces
			//	= "Custom_DbTriggerName_RideHasEnoughFreePlaces_OnRide";
		}
	}
}

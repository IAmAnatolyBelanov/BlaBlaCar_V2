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
					.HasDatabaseName(DbConstants.IndexName_Reservation_UniqueIfActive);

				builder.HasOne(x => x.Leg)
					.WithMany()
					.HasForeignKey(x => x.LegId);

				//				// https://chat.openai.com/c/23535a19-c965-4287-b4d1-acc488ec1ef3
				//				builder.ToTable(b
				//					=> b.HasCheckConstraint(DbConstants.ConstraintName_Reservation_EnoughFreePlaces, $@"
				//(SELECT SUM(reserv.""{nameof(Reservation.Count)}"") FROM ""{nameof(ApplicationContext.Rides)}"" AS ride
				//JOIN ""{nameof(ApplicationContext.Legs)}"" AS leg ON leg.""{nameof(Leg.RideId)}"" = ride.""{nameof(Ride.Id)}""
				//JOIN ""{nameof(ApplicationContext.Reservations)}"" reserv ON reserv.""{nameof(Reservation.LegId)}"" = leg.""{nameof(Leg.Id)}"" AND reserv.""{nameof(Reservation.IsActive)}"" IS TRUE AND reserv.""{nameof(Reservation.Id)}"" = ""{nameof(Reservation.Id)}"")
				//<=
				//(SELECT MAX(ride.""{nameof(Ride.AvailablePlacesCount)}"") FROM ""{nameof(ApplicationContext.Rides)}"" AS ride
				//JOIN ""{nameof(ApplicationContext.Legs)}"" AS leg ON leg.""{nameof(Leg.RideId)}"" = ride.""{nameof(Ride.Id)}""
				//JOIN ""{nameof(ApplicationContext.Reservations)}"" reserv ON reserv.""{nameof(Reservation.LegId)}"" = leg.""{nameof(Leg.Id)}"" AND reserv.""{nameof(Reservation.IsActive)}"" IS TRUE AND reserv.""{nameof(Reservation.Id)}"" = ""{nameof(Reservation.Id)}"")
				//"));

				//				builder.ToTable(t =>
				//				{
				//					t.HasTrigger()
				//				});
			});
		}
	}

	public static class DbConstants
	{
		public static readonly IReadOnlyDictionary<string, string> AllConstants
			= typeof(DbConstants).GetAllStringConstants().ToDictionary();

		public static readonly IReadOnlySet<string> AllConstantValues
			= AllConstants.Values.ToHashSet();

		public const string IndexName_Reservation_UniqueIfActive
			= "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive";

		public const string ConstraintName_Reservation_EnoughFreePlaces
			= "Custom_DbConstraintName_Reservations_EnoughFreePlaces";
	}
}

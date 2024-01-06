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
				//builder.HasNoKey();
				//builder.HasIndex(x => new { x.MasterLegId, x.SubLegId });

				builder.HasIndex(x => x.MasterLegId);
				builder.HasIndex(x => x.SubLegId);

				builder.HasOne(x => x.MasterLeg)
					.WithMany()
					.HasForeignKey(x => x.MasterLegId);

				builder.HasOne(x => x.SubLeg)
					.WithMany()
					.HasForeignKey(x => x.SubLegId);
			});
		}
	}
}

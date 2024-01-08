﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WebApi.DataAccess;

#nullable disable

namespace WebApi.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    [Migration("20240107204756_ReservationIndexRename")]
    partial class ReservationIndexRename
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "postgis");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("WebApi.Models.CompositeLeg", b =>
                {
                    b.Property<Guid>("MasterLegId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("SubLegId")
                        .HasColumnType("uuid");

                    b.HasKey("MasterLegId", "SubLegId");

                    b.HasIndex("MasterLegId");

                    b.HasIndex("SubLegId");

                    b.ToTable("CompositeLegs");
                });

            modelBuilder.Entity("WebApi.Models.Leg", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("empty");

                    b.Property<DateTimeOffset>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Point>("From")
                        .IsRequired()
                        .HasColumnType("geography (point)");

                    b.Property<int>("PriceInRub")
                        .HasColumnType("integer");

                    b.Property<Guid>("RideId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Point>("To")
                        .IsRequired()
                        .HasColumnType("geography (point)");

                    b.HasKey("Id");

                    b.HasIndex("EndTime");

                    b.HasIndex("From");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("From"), "GIST");

                    b.HasIndex("RideId");

                    b.HasIndex("StartTime");

                    b.HasIndex("To");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("To"), "GIST");

                    b.ToTable("Legs");
                });

            modelBuilder.Entity("WebApi.Models.Reservation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Count")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("CreateDateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<Guid>("LegId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("LegId", "UserId")
                        .IsUnique()
                        .HasDatabaseName("Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive")
                        .HasFilter("\"IsActive\" IS TRUE");

                    b.ToTable("Reservations");
                });

            modelBuilder.Entity("WebApi.Models.Ride", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AvailablePlacesCount")
                        .HasColumnType("integer");

                    b.Property<decimal>("DriverId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Rides");
                });

            modelBuilder.Entity("WebApi.Models.CompositeLeg", b =>
                {
                    b.HasOne("WebApi.Models.Leg", "MasterLeg")
                        .WithMany()
                        .HasForeignKey("MasterLegId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebApi.Models.Leg", "SubLeg")
                        .WithMany()
                        .HasForeignKey("SubLegId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MasterLeg");

                    b.Navigation("SubLeg");
                });

            modelBuilder.Entity("WebApi.Models.Leg", b =>
                {
                    b.HasOne("WebApi.Models.Ride", "Ride")
                        .WithMany()
                        .HasForeignKey("RideId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ride");
                });

            modelBuilder.Entity("WebApi.Models.Reservation", b =>
                {
                    b.HasOne("WebApi.Models.Leg", "Leg")
                        .WithMany()
                        .HasForeignKey("LegId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Leg");
                });
#pragma warning restore 612, 618
        }
    }
}
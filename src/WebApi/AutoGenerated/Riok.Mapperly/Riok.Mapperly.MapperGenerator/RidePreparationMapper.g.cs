﻿// <auto-generated />
#nullable enable
namespace WebApi.Models
{
    public partial class RidePreparationMapper
    {
        private partial void ToDtoAuto(global::WebApi.Models.Ride ride, global::WebApi.Models.RidePreparationDto dto)
        {
            dto.Id = ride.Id;
            dto.DriverId = ride.DriverId;
            dto.AvailablePlacesCount = ride.AvailablePlacesCount;
        }

        private partial void FromDtoAuto(global::WebApi.Models.RidePreparationDto dto, global::WebApi.Models.Ride ride)
        {
            ride.Id = dto.Id;
            ride.DriverId = dto.DriverId;
            ride.AvailablePlacesCount = dto.AvailablePlacesCount;
        }

        private partial void BetweenDtosAuto(global::WebApi.Models.RidePreparationDto from, global::WebApi.Models.RidePreparationDto to)
        {
            to.Id = from.Id;
            to.DriverId = from.DriverId;
            to.Legs = from.Legs;
            to.AvailablePlacesCount = from.AvailablePlacesCount;
        }

        private partial void BetweenEntitiesAuto(global::WebApi.Models.Ride from, global::WebApi.Models.Ride to)
        {
            to.Id = from.Id;
            to.DriverId = from.DriverId;
            to.AvailablePlacesCount = from.AvailablePlacesCount;
            to.Status = from.Status;
        }
    }
}
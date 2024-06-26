﻿// <auto-generated />
#nullable enable
namespace WebApi.Models
{
    public partial class RideMapper
    {
        private partial void ToRide(global::WebApi.Models.RideDto src, global::WebApi.Models.Ride target)
        {
            target.Id = src.Id;
            target.AuthorId = src.AuthorId;
            target.DriverId = src.DriverId;
            target.Created = src.Created;
            target.AvailablePlacesCount = src.AvailablePlacesCount;
            target.ValidationMethod = src.ValidationMethod;
            target.ValidationTimeBeforeDeparture = src.ValidationTimeBeforeDeparture;
            target.AfterRideValidationTimeoutAction = src.AfterRideValidationTimeoutAction;
        }

        private partial void ToRideDto(global::WebApi.Models.Ride src, global::WebApi.Models.RideDto target)
        {
            target.Id = src.Id;
            target.AuthorId = src.AuthorId;
            target.DriverId = src.DriverId;
            target.Created = src.Created;
            target.AvailablePlacesCount = src.AvailablePlacesCount;
            target.ValidationMethod = src.ValidationMethod;
            target.ValidationTimeBeforeDeparture = src.ValidationTimeBeforeDeparture;
            target.AfterRideValidationTimeoutAction = src.AfterRideValidationTimeoutAction;
        }
    }
}
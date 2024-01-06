﻿// <auto-generated />
#nullable enable
namespace WebApi.Models
{
    public partial class LegDtoMapper
    {
        private partial void ToDtoAuto(global::WebApi.Models.Leg leg, global::WebApi.Models.LegDto dto)
        {
            dto.Id = leg.Id;
            dto.RideId = leg.RideId;
            dto.From = (global::WebApi.Models.FormattedPoint)leg.From;
            dto.To = (global::WebApi.Models.FormattedPoint)leg.To;
            dto.StartTime = leg.StartTime;
            dto.EndTime = leg.EndTime;
            dto.PriceInRub = leg.PriceInRub;
            dto.Description = leg.Description;
        }

        private partial void FromDtoAuto(global::WebApi.Models.LegDto legDto, global::WebApi.Models.Leg leg)
        {
            leg.Id = legDto.Id;
            leg.RideId = legDto.RideId;
            leg.From = (global::NetTopologySuite.Geometries.Point)legDto.From;
            leg.To = (global::NetTopologySuite.Geometries.Point)legDto.To;
            leg.StartTime = legDto.StartTime;
            leg.EndTime = legDto.EndTime;
            leg.PriceInRub = legDto.PriceInRub;
            leg.Description = legDto.Description;
        }

        private partial void BetweenDtosAuto(global::WebApi.Models.LegDto from, global::WebApi.Models.LegDto to)
        {
            to.Id = from.Id;
            to.Ride = from.Ride;
            to.RideId = from.RideId;
            to.From = from.From;
            to.To = from.To;
            to.StartTime = from.StartTime;
            to.EndTime = from.EndTime;
            to.PriceInRub = from.PriceInRub;
            to.Description = from.Description;
        }

        private partial void BetweenEntitiesAuto(global::WebApi.Models.Leg from, global::WebApi.Models.Leg to)
        {
            to.Id = from.Id;
            to.Ride = from.Ride;
            to.RideId = from.RideId;
            to.From = from.From;
            to.To = from.To;
            to.StartTime = from.StartTime;
            to.EndTime = from.EndTime;
            to.PriceInRub = from.PriceInRub;
            to.Description = from.Description;
        }
    }
}
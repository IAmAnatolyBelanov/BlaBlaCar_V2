﻿// <auto-generated />
#nullable enable
namespace WebApi.Models
{
    public partial class ReservationDtoMapper
    {
        private partial void ToDtoAuto(global::WebApi.Models.Reservation entity, global::WebApi.Models.ReservationDto dto)
        {
            dto.Id = entity.Id;
            dto.LegId = entity.LegId;
            dto.UserId = entity.UserId;
            dto.IsActive = entity.IsActive;
            dto.CreateDateTime = entity.CreateDateTime;
            dto.Count = entity.Count;
        }

        private partial void FromDtoAuto(global::WebApi.Models.ReservationDto dto, global::WebApi.Models.Reservation entity)
        {
            entity.Id = dto.Id;
            entity.LegId = dto.LegId;
            entity.UserId = dto.UserId;
            entity.IsActive = dto.IsActive;
            entity.CreateDateTime = dto.CreateDateTime;
            entity.Count = dto.Count;
        }

        private partial void BetweenDtosAuto(global::WebApi.Models.ReservationDto from, global::WebApi.Models.ReservationDto to)
        {
            to.Id = from.Id;
            to.LegId = from.LegId;
            to.Leg = from.Leg;
            to.UserId = from.UserId;
            to.IsActive = from.IsActive;
            to.CreateDateTime = from.CreateDateTime;
            to.Count = from.Count;
        }

        private partial void BetweenEntitiesAuto(global::WebApi.Models.Reservation from, global::WebApi.Models.Reservation to)
        {
            to.Id = from.Id;
            to.LegId = from.LegId;
            to.Leg = from.Leg;
            to.UserId = from.UserId;
            to.IsActive = from.IsActive;
            to.CreateDateTime = from.CreateDateTime;
            to.Count = from.Count;
        }
    }
}
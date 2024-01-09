using Newtonsoft.Json;

using Riok.Mapperly.Abstractions;

using System.Diagnostics.CodeAnalysis;

namespace WebApi.Models
{
	public class LegDto
	{
		public Guid Id { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public RideDto Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public PlaceAndTime From { get; set; }
		public PlaceAndTime To { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public TimeSpan Duration => To.DateTime - From.DateTime;
		public int PriceInRub { get; set; }
		public string Description { get; set; } = default!;
		public int? FreePlaces { get; set; }
	}

	public struct PlaceAndTime
	{
		public FormattedPoint Point { get; set; }
		public DateTimeOffset DateTime { get; set; }

		public static bool operator ==(PlaceAndTime left, PlaceAndTime right)
			=> left.Point == right.Point && left.DateTime == right.DateTime;
		public static bool operator !=(PlaceAndTime left, PlaceAndTime right)
			=> !(left == right);

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj == null) return false;
			if (obj is not PlaceAndTime other)
				return false;

			return this == other;
		}

		public override int GetHashCode() => HashCode.Combine(Point, DateTime);

		public override string ToString()
			=> $"{{\"{nameof(Point)}\":{Point},\"{nameof(DateTime)}\":{JsonConvert.SerializeObject(DateTime)}}}";
	}

	public interface ILegDtoMapper : IBaseMapper<Leg, LegDto>
	{
	}

	[Mapper]
	public partial class LegDtoMapper : BaseMapper<Leg, LegDto>, ILegDtoMapper
	{
		private readonly Lazy<IRideDtoMapper> _rideDtoMpper;

		public LegDtoMapper(Lazy<IRideDtoMapper> rideDtoMpper)
			: base(() => new(), () => new())
		{
			_rideDtoMpper = rideDtoMpper;
		}

		[MapperIgnoreTarget(nameof(LegDto.Ride))]
		[MapperIgnoreTarget(nameof(LegDto.From))]
		[MapperIgnoreTarget(nameof(LegDto.To))]
		private partial void ToDtoAuto(Leg leg, LegDto dto);

		[MapperIgnoreTarget(nameof(Leg.Ride))]
		[MapperIgnoreTarget(nameof(Leg.From))]
		[MapperIgnoreTarget(nameof(Leg.To))]
		private partial void FromDtoAuto(LegDto legDto, Leg leg);

		private partial void BetweenDtosAuto(LegDto from, LegDto to);
		private partial void BetweenEntitiesAuto(Leg from, Leg to);


		protected override void BetweenDtos(LegDto from, LegDto to)
			=> BetweenDtosAuto(from, to);

		protected override void BetweenEntities(Leg from, Leg to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(LegDto dto, Leg entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.From = dto.From.Point.ToPoint();
			entity.To = dto.To.Point.ToPoint();
			entity.StartTime = dto.From.DateTime;
			entity.EndTime = dto.To.DateTime;

			if (dto.Ride is not null)
				entity.Ride = _rideDtoMpper.Value.FromDto(dto.Ride, mappedObjects);
		}

		protected override void ToDtoAbstract(Leg entity, LegDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.From = new PlaceAndTime
			{
				Point = FormattedPoint.FromPoint(entity.From),
				DateTime = entity.StartTime,
			};
			dto.To = new PlaceAndTime
			{
				Point = FormattedPoint.FromPoint(entity.To),
				DateTime = entity.EndTime,
			};

			if (entity.Ride is not null)
				dto.Ride = _rideDtoMpper.Value.ToDto(entity.Ride, mappedObjects);
		}
	}
}

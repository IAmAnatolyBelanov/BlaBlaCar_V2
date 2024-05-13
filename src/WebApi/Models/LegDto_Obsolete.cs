using Newtonsoft.Json;

using Riok.Mapperly.Abstractions;

using System.Diagnostics.CodeAnalysis;

namespace WebApi.Models
{
	public class LegDto_Obsolete
	{
		public Guid Id { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public RidePreparationDto_Obsolete Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public PlaceAndTime From { get; set; }
		public PlaceAndTime To { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public TimeSpan Duration => To.DateTime - From.DateTime;
		public string Description { get; set; } = default!;


		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public Guid? NextLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto_Obsolete? NextLeg { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public Guid? PreviousLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto_Obsolete? PreviousLeg { get; set; }
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

	public interface ILegDtoMapper : IBaseMapper<Leg_Obsolete, LegDto_Obsolete>
	{
	}

	[Mapper]
	public partial class LegDtoMapper : BaseMapper<Leg_Obsolete, LegDto_Obsolete>, ILegDtoMapper
	{
		private readonly Lazy<IRideDtoMapper> _rideDtoMapper;
		private readonly Lazy<IRidePreparationDtoMapper> _ridePreparationDtoMapper;

		public LegDtoMapper(
			Lazy<IRideDtoMapper> rideDtoMapper,
			Lazy<IRidePreparationDtoMapper> ridePreparationDtoMapper)
			: base(() => new(), () => new())
		{
			_rideDtoMapper = rideDtoMapper;
			_ridePreparationDtoMapper = ridePreparationDtoMapper;
		}

		[MapperIgnoreTarget(nameof(LegDto_Obsolete.Ride))]
		[MapperIgnoreTarget(nameof(LegDto_Obsolete.From))]
		[MapperIgnoreTarget(nameof(LegDto_Obsolete.To))]
		[MapperIgnoreTarget(nameof(LegDto_Obsolete.PreviousLeg))]
		[MapperIgnoreTarget(nameof(LegDto_Obsolete.NextLeg))]
		private partial void ToDtoAuto(Leg_Obsolete leg, LegDto_Obsolete dto);

		[MapperIgnoreTarget(nameof(Leg_Obsolete.Ride))]
		[MapperIgnoreTarget(nameof(Leg_Obsolete.From))]
		[MapperIgnoreTarget(nameof(Leg_Obsolete.To))]
		[MapperIgnoreTarget(nameof(Leg_Obsolete.PreviousLeg))]
		[MapperIgnoreTarget(nameof(Leg_Obsolete.NextLeg))]
		private partial void FromDtoAuto(LegDto_Obsolete legDto, Leg_Obsolete leg);

		private partial void BetweenDtosAuto(LegDto_Obsolete from, LegDto_Obsolete to);
		private partial void BetweenEntitiesAuto(Leg_Obsolete from, Leg_Obsolete to);


		protected override void BetweenDtos(LegDto_Obsolete from, LegDto_Obsolete to)
			=> BetweenDtosAuto(from, to);

		protected override void BetweenEntities(Leg_Obsolete from, Leg_Obsolete to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(LegDto_Obsolete dto, Leg_Obsolete entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.From = dto.From.Point.ToPoint();
			entity.To = dto.To.Point.ToPoint();
			entity.StartTime = dto.From.DateTime;
			entity.EndTime = dto.To.DateTime;

			entity.Ride = dto.Ride is null
				? default!
				: _ridePreparationDtoMapper.Value.FromDto(dto.Ride, mappedObjects);
			entity.PreviousLeg = dto.PreviousLeg is null
				? default
				: FromDto(dto.PreviousLeg, mappedObjects);
			entity.NextLeg = dto.NextLeg is null
				? default
				: FromDto(dto.NextLeg, mappedObjects);
		}

		protected override void ToDtoAbstract(Leg_Obsolete entity, LegDto_Obsolete dto, IDictionary<object, object> mappedObjects)
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

			dto.Ride = entity.Ride is null
				? default!
				: _rideDtoMapper.Value.ToDto(entity.Ride, mappedObjects);
			dto.PreviousLeg = entity.PreviousLeg is null
				? default
				: ToDto(entity.PreviousLeg, mappedObjects);
			dto.NextLeg = entity.NextLeg is null
				? default
				: ToDto(entity.NextLeg, mappedObjects);
		}
	}

	public class LegDtoTimeFromComparer : IComparer<LegDto_Obsolete>
	{
		public static LegDtoTimeFromComparer Instance = new LegDtoTimeFromComparer();

		private LegDtoTimeFromComparer() { }

		public int Compare(LegDto_Obsolete? x, LegDto_Obsolete? y)
		{
			ArgumentNullException.ThrowIfNull(x);
			ArgumentNullException.ThrowIfNull(y);

			return x.From.DateTime.CompareTo(y.To.DateTime);
		}
	}
}

using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class LegDto
	{
		public Guid Id { get; set; }
		public RideDto Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public FormattedPoint From { get; set; }
		public FormattedPoint To { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset EndTime { get; set; }
		public int PriceInRub { get; set; }
		public string Description { get; set; } = default!;
	}

	public interface ILegDtoMapper
	{
		LegDto FromDto(Leg from);
		Leg FromDto(LegDto from);
		void FromDto(LegDto from, Leg to);
		void ToDto(Leg from, LegDto to);
	}

	[Mapper]
	public partial class LegDtoMapper : ILegDtoMapper
	{
		private readonly Lazy<IRideDtoMapper> _rideDtoMpper;

		public LegDtoMapper(Lazy<IRideDtoMapper> rideDtoMpper)
		{
			_rideDtoMpper = rideDtoMpper;
		}

		[MapperIgnoreTarget(nameof(Leg.Ride))]
		private partial void FromDtoInternal(LegDto from, Leg to);
		public void FromDto(LegDto from, Leg to)
		{
			FromDtoInternal(from, to);
			if (from.Ride is not null)
				to.Ride = _rideDtoMpper.Value.FromDto(from.Ride);
		}
		public Leg FromDto(LegDto from)
		{
			var result = new Leg();
			FromDto(from, result);
			return result;
		}

		[MapperIgnoreTarget(nameof(LegDto.Ride))]
		private partial void ToDtoInternal(Leg from, LegDto to);
		public void ToDto(Leg from, LegDto to)
		{
			ToDtoInternal(from, to);
			if (from.Ride is not null)
				to.Ride = _rideDtoMpper.Value.ToDto(from.Ride);
		}
		public LegDto FromDto(Leg from)
		{
			var result = new LegDto();
			ToDto(from, result);
			return result;
		}
	}
}

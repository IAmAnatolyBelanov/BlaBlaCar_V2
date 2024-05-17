using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface ILegDtoMapper
{
	IReadOnlyList<Leg> ToLegs(RideDto rideDto);
}

[Mapper]
public partial class LegDtoMapper : ILegDtoMapper
{
	public IReadOnlyList<Leg> ToLegs(RideDto rideDto)
	{
		var result = new Leg[rideDto.Legs.Count];
		for (int i = 0; i < rideDto.Legs.Count; i++)
		{
			var leg = ToLeg(rideDto.Legs[i]);
			leg.RideId = rideDto.Id;
			result[i] = leg;
		}
		return result;
	}

	private partial Leg ToLeg(LegDto dto);
}

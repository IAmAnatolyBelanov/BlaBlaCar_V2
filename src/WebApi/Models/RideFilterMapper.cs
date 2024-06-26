using Riok.Mapperly.Abstractions;
using WebApi.Models.ControllersModels.RideControllerModels;

namespace WebApi.Models;

public interface IRideFilterMapper
{
	RideDbFilter MapToDbFilter(RideFilter filter);
	RideDbCountsFilter MapToDbCountsFilter(RideFilter filter);
}

[Mapper]
public partial class RideFilterMapper : IRideFilterMapper
{
	public RideDbFilter MapToDbFilter(RideFilter filter)
	{
		var result = new RideDbFilter();
		MapToDbFilter(filter, result);
		return result;
	}

	public RideDbCountsFilter MapToDbCountsFilter(RideFilter filter)
	{
		var result = new RideDbCountsFilter();
		MapToDbFilter(filter, result);
		return result;
	}

	private partial void MapToDbFilter(RideFilter src, RideDbFilter target);
}
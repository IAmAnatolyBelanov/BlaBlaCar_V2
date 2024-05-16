using Riok.Mapperly.Abstractions;
using WebApi.Models.ControllersModels.RideControllerModels;

namespace WebApi.Models;

[Mapper]
public partial class RideFilterMapper
{
	public RideDbFilter MapToDbFilter(RideFilter filter)
	{
		var result = new RideDbFilter();
		MapToDbFilter(filter, result);
		return result;
	}

	private partial void MapToDbFilter(RideFilter src, RideDbFilter target);
}
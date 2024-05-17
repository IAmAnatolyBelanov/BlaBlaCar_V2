using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IWaypointMapper
{
	IReadOnlyList<Waypoint> ToWaypoints(RideDto rideDto);
	WaypointDto ToWaypointDto(Waypoint waypoint);
}

[Mapper]
public partial class WaypointMapper : IWaypointMapper
{
	public IReadOnlyList<Waypoint> ToWaypoints(RideDto rideDto)
	{
		var result = new Waypoint[rideDto.Waypoints.Count];
		for (int i = 0; i < rideDto.Waypoints.Count; i++)
		{
			var waypoint = ToWaypoint(rideDto.Waypoints[i]);
			waypoint.RideId = rideDto.Id;
			result[i] = waypoint;
		}
		return result;
	}

	public partial WaypointDto ToWaypointDto(Waypoint waypoint);

	private partial Waypoint ToWaypoint(WaypointDto dto);
}
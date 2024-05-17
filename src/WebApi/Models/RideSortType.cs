namespace WebApi.Models;

public enum RideSortType
{
	Unknown = 0,
	ByPrice,
	ByStartPointDistance,
	ByEndPointDistance,
	ByStartTime,
	ByEndTime,
	ByFreeSeatsCount,
}
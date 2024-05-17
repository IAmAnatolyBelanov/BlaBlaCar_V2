using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface ISearchRideResponseMapper
{
	SearchRideResponse MapToResponse(SearchRideDbResponse dbResponse);
}

[Mapper]
public partial class SearchRideResponseMapper : ISearchRideResponseMapper
{
	public SearchRideResponse MapToResponse(SearchRideDbResponse dbResponse)
	{
		var ride = new RideDto();

		MapToResponse(dbResponse, ride);

		var paymentMethods = new List<PaymentMethod>();
		if (dbResponse.IsCashPaymentMethodAvailable)
			paymentMethods.Add(PaymentMethod.Cash);
		if (dbResponse.IsCashlessPaymentMethodAvailable)
			paymentMethods.Add(PaymentMethod.Cashless);

		ride.PaymentMethods = paymentMethods;

		var departure = new WaypointDto
		{
			Arrival = dbResponse.FromArrival,
			Departure = dbResponse.FromDeparture,
			FullName = dbResponse.FromFullName,
			NameToCity = dbResponse.FromNameToCity,
			Point = FormattedPoint.FromPoint(dbResponse.FromPoint),
		};

		var arrival = new WaypointDto
		{
			Arrival = dbResponse.ToArrival,
			Departure = dbResponse.ToDeparture,
			FullName = dbResponse.ToFullName,
			NameToCity = dbResponse.ToNameToCity,
			Point = FormattedPoint.FromPoint(dbResponse.ToPoint),
		};

		var result = new SearchRideResponse();

		MapToResponse(dbResponse, result);
		result.Ride = ride;
		result.WaypointDeparture = departure;
		result.WaypointArrival = arrival;
		result.FreePlaces = dbResponse.TotalAvailablePlacesCount - dbResponse.AlreadyReservedSeatsCount;

		return result;
	}

	[MapProperty(nameof(SearchRideDbResponse.RideId), nameof(RideDto.Id))]
	[MapProperty(nameof(SearchRideDbResponse.TotalAvailablePlacesCount), nameof(RideDto.AvailablePlacesCount))]
	private partial void MapToResponse(SearchRideDbResponse source, RideDto target);

	[MapProperty(nameof(SearchRideDbResponse.Price), nameof(SearchRideResponse.PriceInRub))]
	[MapProperty(nameof(SearchRideDbResponse.FromDistanceKilometers), nameof(SearchRideResponse.DepartureDistanceKilometers))]
	[MapProperty(nameof(SearchRideDbResponse.ToDistanceKilometers), nameof(SearchRideResponse.ArrivalDistanceKilometers))]
	private partial void MapToResponse(SearchRideDbResponse source, SearchRideResponse target);
}
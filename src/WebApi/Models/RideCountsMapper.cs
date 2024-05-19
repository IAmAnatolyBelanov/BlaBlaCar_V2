using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IRideCountsMapper
{
	RideCounts ToCounts(RideDbCounts dbCounts, RideDbCountsFilter filter);
}

[Mapper]
public partial class RideCountsMapper : IRideCountsMapper
{
	public RideCounts ToCounts(RideDbCounts dbCounts, RideDbCountsFilter filter)
	{
		var result = new RideCounts();

		ToCounts(dbCounts, result);

		result.PaymentMethodCounts = new Dictionary<PaymentMethod, long>
		{
			[PaymentMethod.Cash] = dbCounts.CashAvailableCount,
			[PaymentMethod.Cashless] = dbCounts.CashlessAvailableCount,
		};

		result.ValidationMethodCounts = new Dictionary<RideValidationMethod, long>
		{
			[RideValidationMethod.ValidationBeforeAccessPassenger] = dbCounts.WithValidationCount,
			[RideValidationMethod.WithoutValidation] = dbCounts.WithoutValidationCount,
		};

		result.DepartureDistanceInKilometersCounts = new Dictionary<float, long>
		{
			[filter.CloseDistanceInKilometers] = dbCounts.CloseDepartureDistanceCount,
			[filter.MiddleDistanceInKilometers] = dbCounts.MiddleDepartureDistanceCount,
			[filter.FarAwayDistanceInKilometers] = dbCounts.FarAwayDepartureDistanceCount,
		};

		result.ArrivalDistanceInKilometersCounts = new Dictionary<float, long>
		{
			[filter.CloseDistanceInKilometers] = dbCounts.CloseArrivalDistanceCount,
			[filter.MiddleDistanceInKilometers] = dbCounts.MiddleArrivalDistanceCount,
			[filter.FarAwayDistanceInKilometers] = dbCounts.FarAwayArrivalDistanceCount,
		};

		return result;
	}

	private partial void ToCounts(RideDbCounts src, RideCounts target);
}
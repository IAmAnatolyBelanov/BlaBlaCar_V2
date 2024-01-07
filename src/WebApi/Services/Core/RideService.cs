using Dapper;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Services.Core
{
	public class RideService
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IRideServiceConfig _config;
		private readonly IRideDtoMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IValidator<IReadOnlyList<LegDto>> _legsCollectionValidatior;

		public RideService(
			IServiceScopeFactory serviceScopeFactory,
			IRideServiceConfig config,
			IRideDtoMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IValidator<IReadOnlyList<LegDto>> legsCollectionValidatior)
		{
			_serviceScopeFactory = serviceScopeFactory;
			_config = config;
			_rideDtoMapper = rideDtoMapper;
			_legDtoMapper = legDtoMapper;
			_legsCollectionValidatior = legsCollectionValidatior;
		}

		public async ValueTask CreateRide(RideDto rideDto, IReadOnlyList<LegDto> legDtos, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			await CreateRide(context, rideDto, legDtos, ct);
		}

		public async ValueTask CreateRide(ApplicationContext context, RideDto rideDto, IReadOnlyList<LegDto> legDtos, CancellationToken ct)
		{
			if (rideDto.Id == default)
				rideDto.Id = Guid.NewGuid();

			for (var i = 0; i < legDtos.Count; i++)
			{
				var legDto = legDtos[i];
				legDto.Ride = rideDto;
				legDto.RideId = rideDto.Id;
				if (legDto.Id == default)
					legDto.Id = Guid.NewGuid();
			}

			var validationResult = _legsCollectionValidatior.Validate(legDtos);
			if (!validationResult.IsValid)
				throw new UserFriendlyException(validationResult);

			var mappedObjects = new Dictionary<object, object>((legDtos.Count + 1) * 2);

			var ride = _rideDtoMapper.FromDto(rideDto, mappedObjects);

			var legs = _legDtoMapper.FromDtoList(legDtos, mappedObjects);

			// Не очевидно, как высчитывать.
			var compositeLegs = new List<CompositeLeg>();
			//for (int i = )

			context.Rides.Add(ride);
			context.Legs.AddRange(legs);
		}

		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await GetRecommendedPriceAsync(context, from, to, ct);
		}

		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct)
		{
			var connection = context.Database.GetDbConnection();

			var maxDistanceInMeters = _config.PriceStatisticsRadiusMeters;
			var lowPercentale = 0 + (1 - _config.PriceStatisticsPercentale) / 2f;
			var highPercentale = 1 - (1 - _config.PriceStatisticsPercentale) / 2f;
			var now = DateTimeOffset.UtcNow;
			var minEndTime = now - _config.PriceStatisticsMaxPastPeriod;

			const string query = $@"
WITH prices AS (
	SELECT ""{nameof(Leg.PriceInRub)}""
	FROM public.""{nameof(ApplicationContext.Legs)}""
	WHERE
		ST_Distance(""{nameof(Leg.From)}"", @{nameof(pointFrom)}) <= @{nameof(maxDistanceInMeters)}
		AND ST_Distance(""{nameof(Leg.To)}"", @{nameof(pointTo)}) <= @{nameof(maxDistanceInMeters)}
		AND ""{nameof(Leg.EndTime)}"" < @{nameof(now)}
		AND ""{nameof(Leg.EndTime)}"" >= @{nameof(minEndTime)}
	ORDER BY ""{nameof(Leg.PriceInRub)}""
)
SELECT
	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		THEN PERCENTILE_CONT(@{nameof(lowPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item1)},
	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		THEN PERCENTILE_CONT(@{nameof(highPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item2)}
FROM prices
LIMIT 1;
";

			var command = new CommandDefinition(
				commandText: query,
				parameters: new
				{
					pointFrom,
					pointTo,
					now,
					minEndTime,
					_config.PriceStatisticsMinRowsCount,
					maxDistanceInMeters,
					lowPercentale,
					highPercentale,
				},
				cancellationToken: ct);
			var result = await connection.QueryFirstAsync<Tuple<double, double>>(command);

			return ((decimal)result.Item1, (decimal)result.Item2);

			throw new NotImplementedException();
		}

		private AsyncServiceScope BuildScope()
			=> _serviceScopeFactory.CreateAsyncScope();
		private ApplicationContext GetDbContext(AsyncServiceScope scope)
			=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();
	}
}

namespace WebApi.Services.Core
{
	public interface IRideServiceConfig
	{
		/// <summary>
		/// Радиус, в котором нужно искать поездки для подсчёта средней цены.
		/// </summary>
		int PriceStatisticsRadiusKilometers { get; }

		/// <summary>
		/// Нижний перцентиль.
		/// </summary>
		float PriceStatisticsLowerPercentile { get; }

		/// <summary>
		/// Средний перцентиль.
		/// </summary>
		float PriceStatisticsMiddlePercentile { get; }

		/// <summary>
		/// Верхний перцентиль.
		/// </summary>
		float PriceStatisticsHigherPercentile { get; }

		int PriceRecommendationMinStep { get; }

		/// <summary>
		/// Насколько старые записи просматриваются для оценки рекомендуемой цены.
		/// </summary>
		TimeSpan PriceStatisticsMaxPastPeriod { get; }

		/// <summary>
		/// Минимальное число записей в бд, при котором можно ориентироваться на статистику и на нормальное распределение.
		/// </summary>
		int PriceStatisticsMinRowsCount { get; }

		/// <summary>
		/// Максимальное число точек в поездке, включая конечные.
		/// </summary>
		int MaxWaypoints { get; }
		int MinPriceInRub { get; }
		int MaxPriceInRub { get; }

		TimeSpan MinTimeForValidationPassengerBeforeDeparture { get; }

		int MinRadiusForSearchKilometers { get; }
		int MaxRadiusForSearchKilometers { get; }

		TimeSpan MaxSearchPeriod { get; }

		int MaxSqlLimit { get; }

		int MinDistanceBetweenPointsInKilometers { get; }

		float CloseDistanceInKilometers { get; }
		float MiddleDistanceInKilometers { get; }
		float FarAwayDistanceInKilometers { get; }

		TimeSpan MinDelayToCurrentTimeForRideCreating { get; }
	}

	public class RideServiceConfig : IBaseConfig, IRideServiceConfig
	{
		public string Position => "RideService";

		/// <inheritdoc/>
		public int PriceStatisticsRadiusKilometers { get; set; } = 50;

		/// <inheritdoc/>
		public float PriceStatisticsLowerPercentile { get; set; } = 0.65f;

		/// <inheritdoc/>
		public float PriceStatisticsMiddlePercentile { get; set; } = 0.75f;

		/// <inheritdoc/>
		public float PriceStatisticsHigherPercentile { get; set; } = 0.85f;

		public int PriceRecommendationMinStep { get; set; } = 50;

		/// <inheritdoc/>
		public TimeSpan PriceStatisticsMaxPastPeriod { get; set; } = TimeSpan.FromDays(3 * 30);

		/// <inheritdoc/>
		public int PriceStatisticsMinRowsCount { get; set; } = 300;

		/// <inheritdoc/>
		public int MaxWaypoints { get; set; } = 30;

		public int MinPriceInRub { get; set; } = 100;
		public int MaxPriceInRub { get; set; } = 300_000;

		public TimeSpan MinTimeForValidationPassengerBeforeDeparture { get; set; } = TimeSpan.FromHours(1);

		public int MinRadiusForSearchKilometers { get; set; } = 10;
		public int MaxRadiusForSearchKilometers { get; set; } = 1000;

		public TimeSpan MaxSearchPeriod { get; set; } = TimeSpan.FromDays(14);

		public int MaxSqlLimit { get; set; } = 5_000;

		public int MinDistanceBetweenPointsInKilometers { get; set; } = 1;

		public float CloseDistanceInKilometers { get; set; } = 5;
		public float MiddleDistanceInKilometers { get; set; } = 15;
		public float FarAwayDistanceInKilometers { get; set; } = 40;

		public TimeSpan MinDelayToCurrentTimeForRideCreating { get; set; } = TimeSpan.FromHours(1);

		public IEnumerable<string> GetValidationErrors()
		{
			if (PriceStatisticsRadiusKilometers <= 0)
				yield return $"{nameof(PriceStatisticsRadiusKilometers)} must be > 0";

			if (PriceStatisticsLowerPercentile <= 0 || PriceStatisticsLowerPercentile >= 1)
				yield return $"{nameof(PriceStatisticsLowerPercentile)} must be in diapason (0, 1). Other words, > 0 and < 1";
			if (PriceStatisticsMiddlePercentile <= 0 || PriceStatisticsMiddlePercentile >= 1)
				yield return $"{nameof(PriceStatisticsMiddlePercentile)} must be in diapason (0, 1). Other words, > 0 and < 1";
			if (PriceStatisticsHigherPercentile <= 0 || PriceStatisticsHigherPercentile >= 1)
				yield return $"{nameof(PriceStatisticsHigherPercentile)} must be in diapason (0, 1). Other words, > 0 and < 1";

			if (PriceRecommendationMinStep <= 0)
				yield return $"{nameof(PriceRecommendationMinStep)} must be > 0";

			if (PriceStatisticsMaxPastPeriod <= TimeSpan.Zero)
				yield return $"{nameof(PriceStatisticsMaxPastPeriod)} must be > 0";

			if (PriceStatisticsMinRowsCount <= 0)
				yield return $"{nameof(PriceStatisticsMinRowsCount)} must be > 0";

			if (MaxWaypoints < 2)
				yield return $"{nameof(MaxWaypoints)} must be >= 2";

			if (MinPriceInRub <= 0)
				yield return $"{nameof(MinPriceInRub)} must be >= 1";

			if (MaxPriceInRub <= 0)
				yield return $"{nameof(MaxPriceInRub)} must be >= 1";

			if (MaxPriceInRub <= MinPriceInRub)
				yield return $"{nameof(MaxPriceInRub)} must be > {MinPriceInRub}";

			if (MinTimeForValidationPassengerBeforeDeparture <= TimeSpan.Zero)
				yield return $"{nameof(MinTimeForValidationPassengerBeforeDeparture)} must be > 0";

			if (MinRadiusForSearchKilometers <= 0)
				yield return $"{nameof(MinRadiusForSearchKilometers)} must be > 0";
			if (MaxRadiusForSearchKilometers <= 0)
				yield return $"{nameof(MaxRadiusForSearchKilometers)} must be > 0";
			if (MaxRadiusForSearchKilometers <= MinRadiusForSearchKilometers)
				yield return $"{nameof(MaxRadiusForSearchKilometers)} must be > {nameof(MinRadiusForSearchKilometers)}";

			if (MaxSearchPeriod <= TimeSpan.Zero)
				yield return $"{nameof(MaxSearchPeriod)} must be > 0";

			if (MaxSqlLimit <= 0)
				yield return $"{nameof(MaxSqlLimit)} must be > 0";

			if (MinDistanceBetweenPointsInKilometers <= 0)
				yield return $"{nameof(MinDistanceBetweenPointsInKilometers)} must be > 0";

			if (CloseDistanceInKilometers <= 0)
				yield return $"{nameof(CloseDistanceInKilometers)} must be > 0";
			if (MiddleDistanceInKilometers <= 0)
				yield return $"{nameof(MiddleDistanceInKilometers)} must be > 0";
			if (FarAwayDistanceInKilometers <= 0)
				yield return $"{nameof(FarAwayDistanceInKilometers)} must be > 0";

			if (MinDelayToCurrentTimeForRideCreating <= TimeSpan.Zero)
				yield return $"{nameof(MinDelayToCurrentTimeForRideCreating)} must be > 0";
		}
	}
}

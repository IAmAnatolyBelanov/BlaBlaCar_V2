namespace WebApi.Services.Core
{
	public interface IRideServiceConfig
	{
		/// <summary>
		/// Радиус, в котором нужно искать поездки для подсчёта средней цены.
		/// </summary>
		int PriceStatisticsRadiusMeters { get; }
		/// <summary>
		/// Перценталь. Если на графике нормального распределения взять самую верхнюю точку, то минус Percentile/2 - нижняя граница, а плюс Percentile/2 - верхняя.
		/// </summary>
		float PriceStatisticsPercentile { get; }
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

		public int MinRadiusForSearchKilometers { get; }
		public int MaxRadiusForSearchKilometers { get; }

		public TimeSpan MaxSearchPeriod { get; }

		public int MaxSqlLimit { get; }
	}

	public class RideServiceConfig : IBaseConfig, IRideServiceConfig
	{
		public string Position => "RideService";

		/// <inheritdoc/>
		public int PriceStatisticsRadiusMeters { get; set; } = 20_000;

		/// <inheritdoc/>
		public float PriceStatisticsPercentile { get; set; } = 0.1f;

		/// <inheritdoc/>
		public TimeSpan PriceStatisticsMaxPastPeriod { get; set; } = TimeSpan.FromDays(3 * 30);

		/// <inheritdoc/>
		public int PriceStatisticsMinRowsCount { get; set; } = 300;

		/// <inheritdoc/>
		public int MaxWaypoints { get; set; } = 10;

		public int MinPriceInRub { get; set; } = 100;
		public int MaxPriceInRub { get; set; } = 300_000;

		public TimeSpan MinTimeForValidationPassengerBeforeDeparture { get; set; } = TimeSpan.FromHours(1);

		public int MinRadiusForSearchKilometers { get; set; } = 10;
		public int MaxRadiusForSearchKilometers { get; set; } = 1000;

		public TimeSpan MaxSearchPeriod { get; set; } = TimeSpan.FromDays(14);

		public int MaxSqlLimit { get; set; } = 5_000;

		public IEnumerable<string> GetValidationErrors()
		{
			if (PriceStatisticsRadiusMeters <= 0)
				yield return $"{nameof(PriceStatisticsRadiusMeters)} must be > 0";

			if (PriceStatisticsPercentile <= 0 || PriceStatisticsPercentile >= 1)
				yield return $"{nameof(PriceStatisticsPercentile)} must be in diapason (0, 1). Other words, > 0 and < 1";

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
		}
	}
}

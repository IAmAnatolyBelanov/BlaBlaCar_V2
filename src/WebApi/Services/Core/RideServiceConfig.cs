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

		/// <summary>
		/// Сколько вариантов должно быть между минимальной и максимальной рекомендованной ценами. Всегда нечётное число.
		/// </summary>
		int RecommendedPriceVariantsMaxCount { get; }

		/// <summary>
		/// Минимально допустимый шаг между рекомендованными ценами.
		/// </summary>
		int RecommendedPriceStepMinValueInRub { get; }
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

		public int MinPriceInRub { get; set; } = 1;
		public int MaxPriceInRub { get; set; } = 100_000;

		/// <inheritdoc/>
		public int RecommendedPriceVariantsMaxCount { get; set; } = 7;

		/// <inheritdoc/>
		public int RecommendedPriceStepMinValueInRub { get; set; } = 50;

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

			if (RecommendedPriceVariantsMaxCount <= 0)
				yield return $"{nameof(RecommendedPriceVariantsMaxCount)} must be >= 1";
			if (RecommendedPriceVariantsMaxCount % 2 == 0)
				yield return $"{nameof(RecommendedPriceVariantsMaxCount)} must be odd";

			if (RecommendedPriceStepMinValueInRub <= 0)
				yield return $"{nameof(RecommendedPriceStepMinValueInRub)} must be >= 1";
		}
	}
}

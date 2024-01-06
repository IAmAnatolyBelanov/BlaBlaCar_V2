using WebApi.Extensions;

namespace WebApi.Services.Core
{
	public interface IRideServiceConfig
	{
		/// <summary>
		/// Радиус, в котором нужно искать поездки для подсчёта средней цены.
		/// </summary>
		int PriceStatisticsRadiusMeters { get; }
		/// <summary>
		/// Перценталь. Если на графике нормального распределения взять самую верхнюю точку, то минус Percentale/2 - нижняя граница, а плюс Percentale/2 - верхняя.
		/// </summary>
		float PriceStatisticsPercentale { get; }
		/// <summary>
		/// Насколько старые записи просматриваются для оценки рекомендуемой цены.
		/// </summary>
		TimeSpan PriceStatisticsMaxPastPeriod { get; }
		/// <summary>
		/// Минимальное число записей в бд, при котором можно ориентироваться на статистику и на нормальное распределение.
		/// </summary>
		int PriceStatisticsMinRowsCount { get; }
	}

	public class RideServiceConfig : IBaseConfig, IRideServiceConfig
	{
		public string Position => "RideService";

		/// <inheritdoc/>
		public int PriceStatisticsRadiusMeters { get; set; } = 20_000;

		/// <inheritdoc/>
		public float PriceStatisticsPercentale { get; set; } = 0.2f;

		/// <inheritdoc/>
		public TimeSpan PriceStatisticsMaxPastPeriod { get; set; } = TimeSpan.FromDays(3 * 30);

		/// <inheritdoc/>
		public int PriceStatisticsMinRowsCount { get; set; } = 300;

		public IEnumerable<string> GetValidationErrors()
		{
			if (PriceStatisticsRadiusMeters <= 0)
				yield return $"{nameof(PriceStatisticsRadiusMeters)} must be > 0";

			if (PriceStatisticsPercentale <= 0 || PriceStatisticsPercentale >= 1)
				yield return $"{nameof(PriceStatisticsPercentale)} must be in diapazone (0, 1). Other words, > 0 and < 1";

			if (PriceStatisticsMaxPastPeriod <= TimeSpan.Zero)
				yield return $"{nameof(PriceStatisticsMaxPastPeriod)} must be > 0";

			if (PriceStatisticsMinRowsCount <= 0)
				yield return $"{nameof(PriceStatisticsMinRowsCount)} must be > 0";
		}
	}
}

using CsvHelper;
using CsvHelper.Configuration;

using System.Globalization;

namespace Tests
{
	/// <summary>
	/// Класс, в котором сохранены данные в csv.
	/// </summary>
	public class CityInfo
	{
		public string FormattedAddress { get; set; } = default!;
		public string Title { get; set; } = default!;
		public string Uri { get; set; } = default!;
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}

	public static class CityInfoManager
	{
		public static IReadOnlyList<CityInfo> AllCities { get; }

		static CityInfoManager()
		{
			var conf = new CsvConfiguration(CultureInfo.InvariantCulture);
			conf.Delimiter = ",";
			conf.NewLine = "\r\n";

			using (var reader = new StreamReader("./CityInfos.csv"))
			using (var csv = new CsvReader(reader, conf))
			{
				AllCities = csv.GetRecords<CityInfo>().ToList();
			}
		}

		private static object _locker = new();
		private static Queue<CityInfo>? _unusedCities;
		public static CityInfo GetUnique()
		{
			if (_unusedCities is null || _unusedCities.Count < 1)
			{
				var allCities = AllCities
					.OrderBy(x => Random.Shared.Next())
					.ToArray();
				_unusedCities = new(allCities);
			}

			lock(_locker)
			{
				return _unusedCities.Dequeue();
			}
		}
	}
}

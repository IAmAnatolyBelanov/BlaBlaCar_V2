using Riok.Mapperly.Abstractions;

namespace WebApi.Models.DriverServiceModels;

public interface IGibddServiceDrivingLicenseResponseMapper
{
	DriverData MapToDriverData(GibddServiceDrivingLicenseResponse source);
}

[Mapper]
public partial class GibddServiceDrivingLicenseResponseMapper : IGibddServiceDrivingLicenseResponseMapper
{
	private static readonly IReadOnlyList<(string Cyrillic, string Latinica)> _misleadingSymbols =
	 [
		("А","A"),
		("В","B"),
		("С","C"),
		("Е","E"),
		("М","M"),
		("Т","T"),
	];

	public DriverData MapToDriverData(GibddServiceDrivingLicenseResponse source)
	{
		var target = new DriverData();
		MapToDriverData(source.Doc, target);
		return target;
	}

	private void MapToDriverData(GibddServiceDrivingLicenseResponse.DocClass source, DriverData target)
	{
		ReadOnlySpan<char> seriesNumber = source.Num;

		target.LicenseSeries = int.Parse(seriesNumber.Slice(start: 0, length: 4));
		target.LicenseNumber = int.Parse(seriesNumber.Slice(start: 4, length: 6));

		target.Issuance = source.Date;
		target.ValidTill = source.Srok;

		target.BirthDate = source.Bdate;

		// Я и вроде не хочу на маппер вешать логику, но и вроде это логика маппера.
		var categories = source.Cat;
		for (int i = 0; i < _misleadingSymbols.Count; i++)
		{
			var pair = _misleadingSymbols[i];
			categories = categories.Replace(pair.Cyrillic, pair.Latinica, StringComparison.OrdinalIgnoreCase);
		}
		// Следствие замены всех в на B.
		categories = categories.Replace("TB", "Tb");

		target.Categories = categories;
	}
}

namespace WebApi.Models;

public class YandexGeocodeResponseDto
{
	// Единственное добавленное поле. Яндекс его не присылает.
	public bool Success { get; set; } = true;

	// Может не содержать ни одного элемента!
	public IReadOnlyList<YandexGeocodeResponseGeoobjectDto> Geoobjects { get; set; } = default!;

	public class YandexGeocodeResponseGeoobjectDto
	{
		public string FormattedAddress { get; set; } = default!;

		/// <summary>
		/// Название вплоть до населённого пункта включительно. Если населённого пункта нет, будет название целиком.
		/// </summary>
		public string ToLocalityName { get; set; } = default!;
		public FormattedPoint Point { get; set; }
	}
}

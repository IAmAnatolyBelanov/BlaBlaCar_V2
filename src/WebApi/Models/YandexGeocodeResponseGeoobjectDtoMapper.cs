using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IYandexGeocodeResponseGeoobjectDtoMapper
{
	YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto ToDto(YandexGeocodeResponse.Featuremember member);
}

[Mapper]
public partial class YandexGeocodeResponseGeoobjectDtoMapper : IYandexGeocodeResponseGeoobjectDtoMapper
{
	public YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto ToDto(YandexGeocodeResponse.Featuremember member)
	{
		var result = new YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto();

		result.FormattedAddress = member.GeoObject.MetaDataProperty.GeocoderMetaData.Address.Formatted;
		result.ToLocalityName = string.Join(", ", GetComponentsTillLocality(member));
		result.Point = member.GeoObject.Point.ToFormattedPoint();

		return result;
	}

	private IEnumerable<string> GetComponentsTillLocality(YandexGeocodeResponse.Featuremember entity)
	{
		for (int i = 0; i < entity.GeoObject.MetaDataProperty.GeocoderMetaData.Address.Components.Length; i++)
		{
			var component = entity.GeoObject.MetaDataProperty.GeocoderMetaData.Address.Components[i];

			if (!component.Name.Equals("locality", StringComparison.InvariantCultureIgnoreCase))
			{
				yield return component.Name;
			}
			else
			{
				yield return component.Name;
				yield break;
			}
		}
	}
}

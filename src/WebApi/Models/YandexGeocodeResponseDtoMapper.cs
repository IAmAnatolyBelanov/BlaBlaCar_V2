using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IYandexGeocodeResponseDtoMapper
{
	YandexGeocodeResponseDto ToDtoLight(YandexGeocodeResponse response);
}

[Mapper]
public partial class YandexGeocodeResponseDtoMapper : IYandexGeocodeResponseDtoMapper
{
	private readonly IYandexGeocodeResponseGeoobjectDtoMapper _geoobjectMapper;

	public YandexGeocodeResponseDtoMapper(IYandexGeocodeResponseGeoobjectDtoMapper geoobjectMapper)
	{
		_geoobjectMapper = geoobjectMapper;
	}

	[MapperIgnoreTarget(nameof(YandexGeocodeResponseDto.Geoobjects))]
	private partial void ToDtoAuto(YandexGeocodeResponse entity, YandexGeocodeResponseDto dto);

	public YandexGeocodeResponseDto ToDtoLight(YandexGeocodeResponse response)
	{
		var result = new YandexGeocodeResponseDto();
		ToDtoAuto(response, result);
		result.Geoobjects = response.Response.GeoObjectCollection.FeatureMember.Select(_geoobjectMapper.ToDto).ToArray();
		return result;
	}
}

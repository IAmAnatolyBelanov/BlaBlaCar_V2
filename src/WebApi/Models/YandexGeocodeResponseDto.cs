using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class YandexGeocodeResponseDto
	{
		// Единственное добавленное поле. Яндекс его не присылает.
		public bool Success { get; set; } = true;

		// Может не содержать ни одного элемента!
		public IReadOnlyList<YandexGeocodeResponseGeoobjectDto> Geoobjects { get; set; } = default!;

		public class YandexGeocodeResponseGeoobjectDto
		{
			public string FormattedAddress { get; set; } = default!;
			public FormattedPoint Point { get; set; }
		}
	}

	public interface IYandexGeocodeResponseDtoMapper : IBaseOneWayMapper<YandexGeocodeResponse, YandexGeocodeResponseDto>
	{
	}

	[Mapper]
	public partial class YandexGeocodeResponseDtoMapper : BaseOneWayMapper<YandexGeocodeResponse, YandexGeocodeResponseDto>, IYandexGeocodeResponseDtoMapper
	{
		private readonly IYandexGeocodeResponseGeoobjectDtoMapper _geoobjectMapper;

		public YandexGeocodeResponseDtoMapper(IYandexGeocodeResponseGeoobjectDtoMapper geoobjectMapper)
			: base(() => new())
		{
			_geoobjectMapper = geoobjectMapper;
		}

		[MapperIgnoreTarget(nameof(YandexGeocodeResponseDto.Geoobjects))]
		private partial void ToDtoAuto(YandexGeocodeResponse entity, YandexGeocodeResponseDto dto);
		private partial void BetweenDtosAuto(YandexGeocodeResponseDto from, YandexGeocodeResponseDto to);

		protected override void BetweenDtos(YandexGeocodeResponseDto from, YandexGeocodeResponseDto to)
			=> BetweenDtosAuto(from, to);
		protected override void ToDtoAbstract(YandexGeocodeResponse entity, YandexGeocodeResponseDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);
			dto.Geoobjects = _geoobjectMapper.ToDtoList(entity.Response.GeoObjectCollection.FeatureMember, mappedObjects);
		}
	}

	public interface IYandexGeocodeResponseGeoobjectDtoMapper : IBaseOneWayMapper<YandexGeocodeResponse.Featuremember, YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto>
	{
	}

	[Mapper]
	public partial class YandexGeocodeResponseGeoobjectDtoMapper : BaseOneWayMapper<YandexGeocodeResponse.Featuremember, YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto>, IYandexGeocodeResponseGeoobjectDtoMapper
	{
		public YandexGeocodeResponseGeoobjectDtoMapper()
			: base(() => new())
		{
		}

		private partial void BetweenDtosAuto(YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto from, YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto to);

		protected override void BetweenDtos(YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto from, YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto to)
			=> BetweenDtosAuto(from, to);

		protected override void ToDtoAbstract(YandexGeocodeResponse.Featuremember entity, YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto dto, IDictionary<object, object> mappedObjects)
		{
			dto.FormattedAddress = entity.GeoObject.MetaDataProperty.GeocoderMetaData.Address.Formatted;
			dto.Point = entity.GeoObject.Point.ToFormattedPoint();
		}
	}
}

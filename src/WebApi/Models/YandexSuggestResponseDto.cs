using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class YandexSuggestResponseDto
	{
		public IReadOnlyList<YandexSuggestResponseDtoResult> Results { get; set; } = default!;

		// Единственное добавленное поле. Яндекс его не присылает.
		public bool Success { get; set; }

		public class YandexSuggestResponseDtoResult
		{
			public string FormattedAddress { get; set; } = default!;
			public string Title { get; set; } = default!;
			public string? SubTitle { get; set; }
			public string Uri { get; set; } = default!;
		}
	}

	public interface IYandexSuggestResponseDtoMapper : IBaseMapper<YandexSuggestResponse, YandexSuggestResponseDto>
	{
	}

	[Mapper]
	public partial class YandexSuggestResponseDtoMapper : BaseMapper<YandexSuggestResponse, YandexSuggestResponseDto>, IYandexSuggestResponseDtoMapper
	{
		private readonly IYandexSuggestResponseDtoResultMapper _yandexSuggestResponseDtoResultMapper;

		public YandexSuggestResponseDtoMapper(IYandexSuggestResponseDtoResultMapper yandexSuggestResponseDtoResultMapper)
			: base(() => throw new NotSupportedException(), () => new())
		{
			_yandexSuggestResponseDtoResultMapper = yandexSuggestResponseDtoResultMapper;
		}

		[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.Results))]
		private partial void ToDtoAuto(YandexSuggestResponse entity, YandexSuggestResponseDto dto);

		protected override void BetweenDtos(YandexSuggestResponseDto from, YandexSuggestResponseDto to) => throw new NotSupportedException();
		protected override void BetweenEntities(YandexSuggestResponse from, YandexSuggestResponse to) => throw new NotSupportedException();
		protected override void FromDtoAbstract(YandexSuggestResponseDto dto, YandexSuggestResponse entity, IDictionary<object, object> mappedObjects) => throw new NotSupportedException();
		protected override void ToDtoAbstract(YandexSuggestResponse entity, YandexSuggestResponseDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.Results = _yandexSuggestResponseDtoResultMapper.ToDtoList(entity.Results, mappedObjects);
		}
	}

	public interface IYandexSuggestResponseDtoResultMapper : IBaseMapper<YandexSuggestResponse.Result, YandexSuggestResponseDto.YandexSuggestResponseDtoResult>
	{
	}

	[Mapper]
	public partial class YandexSuggestResponseDtoResultMapper : BaseMapper<YandexSuggestResponse.Result, YandexSuggestResponseDto.YandexSuggestResponseDtoResult>, IYandexSuggestResponseDtoResultMapper
	{
		public YandexSuggestResponseDtoResultMapper()
			: base(() => throw new NotSupportedException(), () => new())
		{
		}

		[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.YandexSuggestResponseDtoResult.Title))]
		[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.YandexSuggestResponseDtoResult.SubTitle))]
		private partial void ToDtoAuto(YandexSuggestResponse.Result entity, YandexSuggestResponseDto.YandexSuggestResponseDtoResult dto);

		protected override void BetweenDtos(YandexSuggestResponseDto.YandexSuggestResponseDtoResult from, YandexSuggestResponseDto.YandexSuggestResponseDtoResult to) => throw new NotSupportedException();
		protected override void BetweenEntities(YandexSuggestResponse.Result from, YandexSuggestResponse.Result to) => throw new NotSupportedException();
		protected override void FromDtoAbstract(YandexSuggestResponseDto.YandexSuggestResponseDtoResult dto, YandexSuggestResponse.Result entity, IDictionary<object, object> mappedObjects) => throw new NotSupportedException();
		protected override void ToDtoAbstract(YandexSuggestResponse.Result entity, YandexSuggestResponseDto.YandexSuggestResponseDtoResult dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.FormattedAddress = entity.Address.FormattedAddress;
			dto.Title = entity.Title.Text;
			dto.SubTitle = entity.Subtitle?.Text;
		}
	}
}

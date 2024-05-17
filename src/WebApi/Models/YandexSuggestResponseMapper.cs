using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IYandexSuggestResponseMapper
{
	YandexSuggestResponseDto ToResponseDto(YandexSuggestResponse response);
}

[Mapper]
public partial class YandexSuggestResponseMapper : IYandexSuggestResponseMapper
{
	private readonly IYandexSuggestResponseDtoMapper _yandexSuggestResponseResultMapper;

	public YandexSuggestResponseMapper(IYandexSuggestResponseDtoMapper yandexSuggestResponseResultMapper)
	{
		_yandexSuggestResponseResultMapper = yandexSuggestResponseResultMapper;
	}

	[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.Results))]
	private partial void ToDto(YandexSuggestResponse entity, YandexSuggestResponseDto dto);

	public YandexSuggestResponseDto ToResponseDto(YandexSuggestResponse entity)
	{
		var result = new YandexSuggestResponseDto();
		ToDto(entity, result);

		result.Results = entity.Results.Select(_yandexSuggestResponseResultMapper.ToDto).ToArray();

		return result;
	}
}

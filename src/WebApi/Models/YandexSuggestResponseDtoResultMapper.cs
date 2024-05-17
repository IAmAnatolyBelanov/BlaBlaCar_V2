using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IYandexSuggestResponseDtoMapper
{
	YandexSuggestResponseDto.YandexSuggestResponseDtoResult ToDto(YandexSuggestResponse.Result suggestResult);
}

[Mapper]
public partial class YandexSuggestResponseDtoResultMapper : IYandexSuggestResponseDtoMapper
{
	[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.YandexSuggestResponseDtoResult.Title))]
	[MapperIgnoreTarget(nameof(YandexSuggestResponseDto.YandexSuggestResponseDtoResult.SubTitle))]
	private partial void ToDtoAuto(YandexSuggestResponse.Result entity, YandexSuggestResponseDto.YandexSuggestResponseDtoResult dto);

	public YandexSuggestResponseDto.YandexSuggestResponseDtoResult ToDto(YandexSuggestResponse.Result suggestResult)
	{
		var result = new YandexSuggestResponseDto.YandexSuggestResponseDtoResult();
		ToDtoAuto(suggestResult, result);

		result.FormattedAddress = suggestResult.Address.FormattedAddress;
		result.Title = suggestResult.Title.Text;
		result.SubTitle = suggestResult.Subtitle?.Text;

		return result;
	}
}

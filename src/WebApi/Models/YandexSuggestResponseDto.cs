namespace WebApi.Models;

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

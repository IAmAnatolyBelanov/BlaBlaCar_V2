using Newtonsoft.Json;

using System.Text.Json.Serialization;

namespace WebApi.Models
{
	// Содержит не все поля в целях экономии ресурсов.
	public class YandexSuggestResponse
	{
		[JsonPropertyName("suggest_reqid")]
		[JsonProperty("suggest_reqid")]
		public string SuggestReqId { get; set; } = default!;
		public Result[] Results { get; set; } = default!;

		// Единственное добавленное поле. Яндекс его не присылает.
		public bool Success { get; set; } = true;

		public class Result
		{
			public Title Title { get; set; } = default!;
			public Subtitle? Subtitle { get; set; }

			public Address1 Address { get; set; } = default!;
			public string Uri { get; set; } = default!;
		}

		public class Title
		{
			public string Text { get; set; } = default!;
		}

		public class Subtitle
		{
			public string Text { get; set; } = default!;
		}

		// Сей нейминг обусловлен тем, что сваггеру не по силам одноимённые классы разрулить.
		public class Address1
		{
			[JsonPropertyName("formatted_address")]
			[JsonProperty("formatted_address")]
			public string FormattedAddress { get; set; } = default!;
		}
	}
}

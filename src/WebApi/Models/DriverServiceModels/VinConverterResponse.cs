using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class VinConverterResponse
{
	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("partner")]
	public PartnerClass? Partner { get; set; }

	[JsonProperty("inquiry")]
	public Inquiry Inquiry { get; set; } = default!;

	public class PartnerClass
	{
		[JsonProperty("status")]
		public int Status { get; set; }

		[JsonProperty("found")]
		public bool Found { get; set; }

		[JsonProperty("result")]
		public Result? Result { get; set; }
	}

	public class Result
	{
		[JsonProperty("brand_model")]
		public string? BrandModel { get; set; }

		[JsonProperty("brand")]
		public string? Brand { get; set; }

		[JsonProperty("model")]
		public string? Model { get; set; }

		[JsonProperty("year")]
		public int? Year { get; set; }

		[JsonProperty("regNumber")]
		public string? RegNumber { get; set; }

		[JsonProperty("vin")]
		public string? Vin { get; set; }

		[JsonProperty("body")]
		public string? Body { get; set; }

		// Это поле в документации было просто null. Вероятнее всего, там строка, но рисковать нет смысла.
		// [JsonProperty("chassis")]
		// public string? Chassis { get; set; }
	}
}

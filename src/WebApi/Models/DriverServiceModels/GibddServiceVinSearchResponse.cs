using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class GibddServiceVinSearchResponse
{
	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("found")]
	public bool Found { get; set; }

	[JsonProperty("utilicazia")]
	public int Utilicazia { get; set; }

	[JsonProperty("utilicaziainfo")]
	public string? UtilicaziaInfo { get; set; }

	[JsonProperty("vehicle")]
	public VehicleClass? Vehicle { get; set; }

	[JsonProperty("vehiclePassport")]
	public VehiclePassportClass? VehiclePassport { get; set; }

	[JsonProperty("inquiry")]
	public Inquiry Inquiry { get; set; } = default!;

	public class VehicleClass
	{
		[JsonProperty("vin")]
		public string? Vin { get; set; }

		[JsonProperty("bodyNumber")]
		public string? BodyNumber { get; set; }

		[JsonProperty("engineNumber")]
		public string? EngineNumber { get; set; }

		[JsonProperty("model")]
		public string? Model { get; set; }

		[JsonProperty("color")]
		public string? Color { get; set; }

		[JsonProperty("year")]
		public string? Year { get; set; }

		[JsonProperty("engineVolume")]
		public string? EngineVolume { get; set; }

		[JsonProperty("powerHp")]
		public string? PowerHp { get; set; }

		[JsonProperty("powerKwt")]
		public string? PowerKwt { get; set; }

		[JsonProperty("category")]
		public string? Category { get; set; }

		[JsonProperty("type")]
		public string? Type { get; set; }

		[JsonProperty("typeinfo")]
		public string? TypeInfo { get; set; }
	}

	public class VehiclePassportClass
	{
		[JsonProperty("number")]
		public string? Number { get; set; }

		[JsonProperty("issue")]
		public string? Issue { get; set; }
	}
}

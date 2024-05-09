using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class Inquiry
{
	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("balance")]
	public double Balance { get; set; }

	[JsonProperty("speed")]
	public long Speed { get; set; }

	[JsonProperty("attempts")]
	public long Attempts { get; set; }
}

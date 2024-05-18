using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class Inquiry
{
	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("balance")]
	public double Balance { get; set; }

	[JsonProperty("speed")]
	public float Speed { get; set; }

	[JsonProperty("attempts")]
	public int Attempts { get; set; }
}

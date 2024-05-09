using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class TaxServiceInnResponse
{
	/// <summary>
	/// Статус.
	/// </summary>
	/// <remarks>
	/// Заявлены коды 200 и 404.
	/// </remarks>
	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("found")]
	public bool Found { get; set; }

	/// <summary>
	/// Инн.
	/// </summary>
	/// <remarks>
	/// Хоть и является long, но сервис api-cloud присылает его как строку.
	/// </remarks>
	[JsonProperty("inn")]
	public string? Inn { get; set; }

	/// <summary>
	/// Сообщение в случае ошибки.
	/// </summary>
	[JsonProperty("message")]
	public string? Message { get; set; }

	[JsonProperty("inquiry")]
	public Inquiry Inquiry { get; set; } = default!;
}

namespace WebApi.Models;

public class CloudApiResponseInfo
{
	private DateTimeOffset created;

	public Guid Id { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
	public string Request { get; set; } = default!;
	public string RequestBasePath { get; set; } = default!;
	public string Response { get; set; } = default!;
}

namespace WebApi.Infrastructure;

public class ErrorDetails
{
	public string Code { get; set; } = default!;
	public string Message { get; set; } = default!;
	public string? AdditionalInfo { get; set; }
}

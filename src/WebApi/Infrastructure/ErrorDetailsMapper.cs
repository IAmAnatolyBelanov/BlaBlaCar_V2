using Riok.Mapperly.Abstractions;

namespace WebApi.Infrastructure;

public interface IErrorDetailsMapper
{
	ErrorDetails ToErrorDetail(ValidationFailure failure);
}

[Mapper]
public partial class ErrorDetailsMapper : IErrorDetailsMapper
{
	public ErrorDetails ToErrorDetail(ValidationFailure failure)
	{
		var result = new ErrorDetails();
		result.Code = failure.ErrorCode;
		result.Message = failure.ErrorMessage;
		result.AdditionalInfo = string.IsNullOrWhiteSpace(failure.PropertyName)
			? null
			: $"{nameof(ValidationFailure.PropertyName)} - {failure.PropertyName}";
		return result;
	}
}

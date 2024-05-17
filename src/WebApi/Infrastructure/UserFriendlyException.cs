using Newtonsoft.Json;

namespace WebApi.Infrastructure
{
	public class UserFriendlyException : Exception
	{
		private static readonly IErrorDetailsMapper _errorDetailsMapper
			= new ErrorDetailsMapper();

		public readonly IReadOnlyList<ErrorDetails> Errors;
		private string? _message;

		public UserFriendlyException(string code, string message)
		{
			Errors = [new() { Message = message, Code = code }];
		}

		public UserFriendlyException(string code, string message, string? additionalInfo)
		{
			Errors = [new() { Message = message, Code = code, AdditionalInfo = additionalInfo }];
		}

		public UserFriendlyException(IReadOnlyList<ValidationFailure> validationFailure)
		{
			Errors = validationFailure.Select(_errorDetailsMapper.ToErrorDetail).ToArray();
		}

		public UserFriendlyException(ValidationResult validationResult)
			: this(validationResult.Errors)
		{
		}

		public override string Message
		{
			get
			{
				if (_message != null)
					return _message;

				var message = JsonConvert.SerializeObject(Errors);
				_message = message;
				return _message;
			}
		}
	}
}

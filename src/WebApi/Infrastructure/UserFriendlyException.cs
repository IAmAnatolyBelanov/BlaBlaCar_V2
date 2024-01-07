using Newtonsoft.Json;

namespace WebApi.Infrastructure
{
	public class UserFriendlyException : Exception
	{
		private static readonly IErrorDetailsMapper _errorDetailsMapper
			= new ErrorDetailsMapper();

		public readonly IReadOnlyList<ErrorDetails> Errors;
		private string? _message;

		public UserFriendlyException(string message)
			: base(message)
		{
			Errors = [new() { Message = message }];
		}

		public UserFriendlyException(string message, string code)
			: base(message)
		{
			Errors = [new() { Message = message, Code = code }];
		}

		public UserFriendlyException(string message, string code, string? additionalInfo)
			: base(message)
		{
			Errors = [new() { Message = message, Code = code, AdditionalInfo = additionalInfo }];
		}

		public UserFriendlyException(IReadOnlyList<ValidationFailure> validationFailure)
		{
			Errors = _errorDetailsMapper.ToDtoListLight(validationFailure)!;
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

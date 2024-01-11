using Newtonsoft.Json;

using System.Runtime.CompilerServices;
using System.Text;

namespace WebApi.Infrastructure
{
	public class UserFriendlyException : Exception
	{
		private static readonly IErrorDetailsMapper _errorDetailsMapper
			= new ErrorDetailsMapper();

		public readonly IReadOnlyList<ErrorDetails> Errors;
		public readonly string? AdditionalInfo;

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

		public UserFriendlyException(IReadOnlyList<ValidationFailure> validationFailures, ErrorInterpolatedStringHandler additionalMessage)
			: this(validationFailures)
		{
			AdditionalInfo = additionalMessage.GetFormattedText();
		}

		public UserFriendlyException(IReadOnlyList<ValidationFailure> validationFailures, string additionalMessage)
			: this(validationFailures)
		{
			AdditionalInfo = additionalMessage;
		}

		public UserFriendlyException(ValidationResult validationResult)
			: this(validationResult.Errors)
		{
		}
		public UserFriendlyException(ValidationResult validationResult, ErrorInterpolatedStringHandler additionalMessage)
			: this(validationResult.Errors, additionalMessage)
		{
		}
		public UserFriendlyException(ValidationResult validationResult, string additionalMessage)
			: this(validationResult.Errors, additionalMessage)
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

	[InterpolatedStringHandler]
	public ref struct ErrorInterpolatedStringHandler
	{
		// Storage for the built-up string
		private StringBuilder _builder;

		public ErrorInterpolatedStringHandler(int literalLength)
		{
			_builder = new StringBuilder(literalLength);
		}

		public void AppendLiteral(string s)
		{
			_builder.Append(s);
		}

		public void AppendFormatted<T>(T t)
		{
			_builder.Append(t?.ToString());
		}

		internal string GetFormattedText() => _builder.ToString();
	}
}

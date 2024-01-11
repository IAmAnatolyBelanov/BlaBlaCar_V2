namespace WebApi.Infrastructure
{
	public class BaseResponse<T>
	{
		protected static readonly IErrorDetailsMapper _errorDetailsMapper
			= new ErrorDetailsMapper();

		public static BaseResponse<T> Empty { get; } = new()
		{
			Success = true,
		};

		public BaseResponse()
		{
		}

		public BaseResponse(T data)
		{
			Data = data;
			Success = true;
		}

		public bool Success { get; set; }
		public T? Data { get; set; }
		public IReadOnlyList<ErrorDetails>? Errors { get; set; }
		public string? AdditionalInfo { get; set; }


		public static implicit operator BaseResponse<T>(ValidationResult? validationResult)
		{
			var errors = validationResult?.IsValid != false
				? Array.Empty<ErrorDetails>()
				: _errorDetailsMapper.ToDtoListLight(validationResult.Errors)!;

			return new BaseResponse<T>()
			{
				Success = false,
				Errors = errors,
			};
		}

		public static implicit operator BaseResponse<T>(T instance)
		{
			return new BaseResponse<T>()
			{
				Data = instance,
				Success = true,
			};
		}
	}

	public class StringResponse : BaseResponse<object>
	{
		public static new StringResponse Empty { get; } = new()
		{
			Success = true,
		};

		public static implicit operator StringResponse(ValidationResult? validationResult)
		{
			var errors = validationResult?.IsValid != false
				? Array.Empty<ErrorDetails>()
				: _errorDetailsMapper.ToDtoListLight(validationResult.Errors)!;

			return new StringResponse()
			{
				Success = false,
				Errors = errors,
			};
		}
	}
}

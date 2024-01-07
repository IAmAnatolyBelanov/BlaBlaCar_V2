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
	}

	public class EmptyResponse : BaseResponse<object>
	{
		public static new EmptyResponse Empty { get; } = new()
		{
			Success = true,
		};

		public static implicit operator EmptyResponse(ValidationResult? validationResult)
		{
			var errors = validationResult?.IsValid != false
				? Array.Empty<ErrorDetails>()
				: _errorDetailsMapper.ToDtoListLight(validationResult.Errors)!;

			return new EmptyResponse()
			{
				Success = false,
				Errors = errors,
			};
		}
	}
}

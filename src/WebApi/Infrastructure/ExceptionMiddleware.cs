using Newtonsoft.Json;

using System.Net;

namespace WebApi.Infrastructure
{
	public class ExceptionMiddleware
	{
		private static readonly string _unknownErrorResponse;
		private static readonly ILogger _logger = Log.ForContext<ExceptionMiddleware>();

		static ExceptionMiddleware()
		{
			var error = new ErrorDetails
			{
				Code = "UnknownError",
				Message = "Unknown error",
			};

			_unknownErrorResponse = JsonConvert.SerializeObject(new BaseResponse<string>
			{
				Errors = [error],
				Success = false,
			});
		}

		private readonly RequestDelegate _next;
		private readonly IErrorDetailsMapper _errorMapper;

		public ExceptionMiddleware(RequestDelegate next, IErrorDetailsMapper errorMapper)
		{
			_next = next;
			_errorMapper = errorMapper;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (UserFriendlyException ex)
			{
				_logger.Error(ex, "UserFriendly error");

				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				context.Response.ContentType = "application/json";

				StringResponse result = new()
				{
					Errors = ex.Errors,
					Success = false,
				};

				await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Fatal error");

				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				context.Response.ContentType = "application/json";

				await context.Response.WriteAsync(_unknownErrorResponse);
			}
		}
	}
}

using Microsoft.AspNetCore.Mvc;

using WebApi.Services.Validators;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class ConstantsController : ControllerBase
	{
		[HttpGet]
		public BaseResponse<IOrderedEnumerable<string>> GetAllValidationCodes()
		{
			var result = ValidationCodes.AllConstants.Keys
				.OrderBy(x => x);
			return BaseResponse.From(result);
		}
	}
}

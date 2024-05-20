using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.ControllersModels;
using WebApi.Services.Driver;

namespace WebApi.Controllers;


[ApiController]
[Route("api/[controller]/[action]")]
public class CarController : ControllerBase
{
	private readonly IDriverService _driverService;

	public CarController(IDriverService driverService)
	{
		_driverService = driverService;
	}

	[HttpPost]
	public async Task<BaseResponse<CarDto?>> SearchCar(SearchByVinRequest request, CancellationToken ct)
	{
		var result = await _driverService.SearchCar(request.Vin, ct);
		return result;
	}
}
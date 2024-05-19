using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.ControllersModels.UserControllerModels;
using WebApi.Models.DriverServiceModels;
using WebApi.Services.Driver;
using WebApi.Services.User;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController
{
	private readonly IUserService _userService;
	private readonly IDriverService _driverService;

	public UserController(IUserService userService, IDriverService driverService)
	{
		_userService = userService;
		_driverService = driverService;

	}

	[HttpPost]
	public async Task<StringResponse> RegisterUser(User user, CancellationToken ct)
	{
		await _userService.RegisterUser(user, ct);
		return StringResponse.Empty;
	}

	[HttpPost]
	public async Task<StringResponse> UpdatePersonData(UpdatePersonDataRequest request, CancellationToken ct)
	{
		await _userService.UpdatePersonData(request.UserId, request.Person, ct);
		return StringResponse.Empty;
	}

	[HttpPost]
	public async Task<StringResponse> UpdateDriverInfo(UpdateDriverInfoRequest request, CancellationToken ct)
	{
		await _driverService.ValidateDriverLicense(request.UserId, request.DriverData, ct);
		return StringResponse.Empty;
	}
}

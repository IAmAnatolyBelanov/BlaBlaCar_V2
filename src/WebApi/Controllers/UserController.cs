using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.ControllersModels.UserControllerModels;
using WebApi.Models.DriverServiceModels;
using WebApi.Services.User;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController
{
	private readonly IUserService _userService;

	public UserController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpPost]
	public async Task RegisterUser(User user, CancellationToken ct)
	{
		await _userService.RegisterUser(user, ct);
	}

	[HttpPost]
	public async Task UpdatePersonData(UpdatePersonDataRequest request, CancellationToken ct)
	{
		await _userService.UpdatePersonData(request.UserId, request.Person, ct);
	}
}

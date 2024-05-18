using WebApi.Models.DriverServiceModels;

namespace WebApi.Models.ControllersModels.UserControllerModels;

public class UpdateDriverInfoRequest
{
	public Guid UserId { get; set; }
	public Driver DriverData { get; set; } = default!;
}
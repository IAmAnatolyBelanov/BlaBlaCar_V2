using WebApi.Models.DriverServiceModels;

namespace WebApi.Models.ControllersModels.UserControllerModels;

public class UpdatePersonDataRequest
{
	public Guid UserId { get; set; }
	public Person Person { get; set; } = default!;
}

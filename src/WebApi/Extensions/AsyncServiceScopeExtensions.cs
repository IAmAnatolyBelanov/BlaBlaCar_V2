using WebApi.DataAccess;

namespace WebApi;

public static class AsyncServiceScopeExtensions
{
	public static ApplicationContext GetDbContext(this AsyncServiceScope scope)
		=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();
}

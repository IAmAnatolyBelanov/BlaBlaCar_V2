using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryWithDb>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;

		public RideServiceTests(TestAppFactoryWithDb factory)
		{
			_provider = factory.Services;
			factory.MigrateDb();
			_fixture = Shared.BuildDefaultFixture();
			_rideService = _provider.GetRequiredService<IRideService>();
		}

	}
}

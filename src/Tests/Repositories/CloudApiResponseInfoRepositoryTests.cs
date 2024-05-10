using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class CloudApiResponseInfoRepositoryTests : BaseRepositoryTest
{
	private readonly ICloudApiResponseInfoRepository _repo;

	public CloudApiResponseInfoRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_repo = _provider.GetRequiredService<ICloudApiResponseInfoRepository>();
	}

	[Fact]
	public async Task InsertTest()
	{
		var fixture = Shared.BuildDefaultFixture();
		var info = fixture.Build<CloudApiResponseInfo>()
			.With(x => x.Response, "{}")
			.Create();

		using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction();
		var result = await _repo.Insert(session, info, CancellationToken.None);
		await session.CommitAsync(CancellationToken.None);

		result.Should().Be(1);
	}

	[Fact]
	public async Task InsertAndGetWithOffsetTest()
	{
		var fixture = Shared.BuildDefaultFixture();
		var generatedInfos = fixture.Build<CloudApiResponseInfo>()
			.With(x => x.Response, "{}")
			.CreateMany(100)
			.ToArray();

		foreach (var info in generatedInfos)
		{
			using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
			{
				await _repo.Insert(session, info, CancellationToken.None);
				await session.CommitAsync(CancellationToken.None);
			}
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.Get(session, limit: generatedInfos.Length - 10, offset: 10, ct: CancellationToken.None);

			result.Should().HaveCount(generatedInfos.Length - 10);
		}
	}
}

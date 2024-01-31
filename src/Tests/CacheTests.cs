using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;
using WebApi.Services.InMemoryCaches;
using WebApi.Services.Redis;

namespace Tests;

public class CacheTests : IClassFixture<TestAppFactoryWithRedis>, IDisposable
{
	private readonly Fixture _fixture;

	private readonly TestAppFactoryWithRedis _appFactory;
	private readonly IServiceProvider _provider;
	private readonly IServiceScope _scope;
	private readonly IRedisCacheService _redis;
	private readonly IInMemoryCache<string, string> _inMemoryCache;
	private const int _inMemoryCacheLimit = 100;

	public CacheTests(TestAppFactoryWithRedis fixture)
	{
		_appFactory = fixture;
		_fixture = Shared.BuildDefaultFixture();
		_provider = fixture.Services;
		_scope = _provider.CreateScope();
		_redis = _scope.ServiceProvider.GetRequiredService<IRedisCacheService>();

		_inMemoryCache = new InMemoryCache<string, string>(new MemoryCacheOptions
		{
			SizeLimit = _inMemoryCacheLimit,
		});
	}

	[Fact(Timeout = 30_000)]
	public async Task TestGettingStringFromRedisAsync()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		await _redis.SetStringAsync(key, value, TimeSpan.FromMinutes(1), CancellationToken.None);

		(var keyExists, var result) = await _redis.TryGetStringAsync(key, CancellationToken.None);

		keyExists.Should().BeTrue();
		result.Should().Be(value);
	}

	[Fact(Timeout = 30_000)]
	public void TestGettingStringFromRedisSync()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		_redis.SetString(key, value, TimeSpan.FromMinutes(1));

		(var keyExists, var result) = _redis.TryGetString(key);

		keyExists.Should().BeTrue();
		result.Should().Be(value);
	}

	[Fact(Timeout = 30_000)]
	public async Task TestGettingNonExistingStringAsync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = await _redis.TryGetStringAsync(key, CancellationToken.None);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}

	[Fact(Timeout = 30_000)]
	public void TestGettingNonExistingStringSync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = _redis.TryGetString(key);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}

	[Fact(Timeout = 30_000)]
	public async Task TestGettingObjectAsync()
	{
		var key = _fixture.Create<string>();
		var obj = _fixture.Create<YandexSuggestResponse>();

		await _redis.SetAsync(key, obj, TimeSpan.FromMinutes(1), CancellationToken.None);

		(var keyExists, var result) = await _redis.TryGetAsync<YandexSuggestResponse>(key, CancellationToken.None);

		keyExists.Should().BeTrue();
		result.Should().BeEquivalentTo(obj);
	}

	[Fact(Timeout = 30_000)]
	public void TestGettingObjectSync()
	{
		var key = _fixture.Create<string>();
		var obj = _fixture.Create<YandexSuggestResponse>();

		_redis.Set(key, obj, TimeSpan.FromMinutes(1));

		(var keyExists, var result) = _redis.TryGet<YandexSuggestResponse>(key);

		keyExists.Should().BeTrue();
		result.Should().BeEquivalentTo(obj);
	}

	[Fact(Timeout = 30_000)]
	public async Task TestGettingNonExistingObjectAsync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = await _redis.TryGetAsync<YandexSuggestResponse>(key, CancellationToken.None);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}
	[Fact(Timeout = 30_000)]
	public async Task TestGettingNonExistingStructAsync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = await _redis.TryGetAsync<FormattedPoint>(key, CancellationToken.None);

		keyExists.Should().BeFalse();
		result.Should().Be(default(FormattedPoint));
	}

	[Fact(Timeout = 30_000)]
	public void TestGettingNonExistingObjectSync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = _redis.TryGet<YandexSuggestResponse>(key);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}
	[Fact(Timeout = 30_000)]
	public void TestGettingNonExistingStructSync()
	{
		var key = _fixture.Create<string>();

		(var keyExists, var result) = _redis.TryGet<FormattedPoint>(key);

		keyExists.Should().BeFalse();
		result.Should().Be(default(FormattedPoint));
	}

	[Fact(Timeout = 30_000)]
	public async Task TestRedisExpirationAsync()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		var lifeTime = TimeSpan.FromMilliseconds(100);

		await _redis.SetStringAsync(key, value, lifeTime, CancellationToken.None);

		await Task.Delay(TimeSpan.FromMilliseconds(lifeTime.TotalMilliseconds * 20));

		(var keyExists, var result) = await _redis.TryGetStringAsync(key, CancellationToken.None);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}

	[Fact(Timeout = 30_000)]
	public void TestRedisExpirationSync()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		var lifeTime = TimeSpan.FromMilliseconds(100);

		_redis.SetString(key, value, lifeTime);

		Thread.Sleep(TimeSpan.FromMilliseconds(lifeTime.TotalMilliseconds * 20));

		(var keyExists, var result) = _redis.TryGetString(key);

		keyExists.Should().BeFalse();
		result.Should().Be(default);
	}

	[Fact(Timeout = 30_000)]
	public async Task TestRedisDeath()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		await _redis.SetStringAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);

		(var exists, _) = await _redis.TryGetStringAsync(key, CancellationToken.None);

		exists.Should().BeTrue();

		_appFactory.RestartContainer(TimeSpan.FromSeconds(5));

		(exists, var result) = await _redis.TryGetStringAsync(key, CancellationToken.None);

		exists.Should().BeTrue();
		result.Should().Be(value);
	}

	[Fact(Timeout = 30_000)]
	public void TestGettingFromInMemoryCache()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		_inMemoryCache.Set(key, value, TimeSpan.FromMinutes(1));

		var exists = _inMemoryCache.TryGetValue(key, out var result);

		exists.Should().BeTrue();
		result.Should().Be(value);
	}

	[Fact(Timeout = 30_000)]
	public async Task TestExpirationInMemory()
	{
		var key = _fixture.Create<string>();
		var value = _fixture.Create<string>();

		var lifetime = TimeSpan.FromMilliseconds(100);

		_inMemoryCache.Set(key, value, lifetime);

		await Task.Delay(TimeSpan.FromMilliseconds(lifetime.TotalMilliseconds * 20));

		var exists = _inMemoryCache.TryGetValue(key, out var result);

		exists.Should().BeFalse();
		result.Should().Be(default);
	}

	public void Dispose()
	{
		_scope.Dispose();
	}

	~CacheTests()
	{
		Dispose();
	}
}

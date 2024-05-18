using Microsoft.AspNetCore.Mvc;

using WebApi.Services.Redis;
using WebApi.Services.Yandex;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class RedisTest
	{
		private readonly IRedisCacheService _redisCacheService;
		private readonly ILogger _logger = Log.ForContext<RedisTest>();
		private readonly ISuggestService _suggestService;

		public RedisTest(IRedisCacheService redisCacheService, ISuggestService suggestService)
		{
			_redisCacheService = redisCacheService;
			_suggestService = suggestService;
		}

		[HttpGet]
		public async ValueTask<Tuple<bool, string?>> Get(string key, CancellationToken ct)
		{
			var res = await _redisCacheService.TryGetStringAsync(key, ct);
			return Tuple.Create(res.Item1, res.Item2);
		}

		[HttpPost]
		public async ValueTask Set(string key, string value, CancellationToken ct)
		{
			await _redisCacheService.SetStringAsync(key, value, TimeSpan.FromMinutes(1), ct);
		}
	}
}

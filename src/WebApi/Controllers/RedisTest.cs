using Microsoft.AspNetCore.Mvc;

using WebApi.Services.Redis;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class RedisTest
	{
		private readonly IRedisCacheService _redisCacheService;

		public RedisTest(IRedisCacheService redisCacheService)
		{
			_redisCacheService = redisCacheService;
		}

		[HttpGet]
		public async ValueTask<Tuple<bool, string?>> Get(string key)
		{
			using (var db = _redisCacheService.Connect())
			{
				var res = await _redisCacheService.TryGetStringAsync(db, key);
				return Tuple.Create(res.Item1, res.Item2);
			}
		}

		[HttpPost]
		public async ValueTask Set(string key, string value)
		{
			using (var db = _redisCacheService.Connect())
				await _redisCacheService.SetStringAsync(db, key, value, TimeSpan.FromMinutes(1));
		}
	}
}

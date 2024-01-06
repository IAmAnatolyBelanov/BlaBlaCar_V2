using Newtonsoft.Json;

using StackExchange.Redis;

namespace WebApi.Services.Redis
{
	public interface IRedisCacheService
	{
		void Clear();
		IRedisDataBase Connect();
		void Set<T>(IRedisDataBase database, string key, T value, TimeSpan expiry);
		Task SetAsync<T>(IRedisDataBase database, string key, T value, TimeSpan expiry);
		void SetString(IRedisDataBase database, string key, string value, TimeSpan expiry);
		Task SetStringAsync(IRedisDataBase database, string key, string value, TimeSpan expiry);
		(bool, T?) TryGet<T>(IRedisDataBase database, string key);
		ValueTask<(bool, T?)> TryGetAsync<T>(IRedisDataBase database, string key);
		(bool, string?) TryGetString(IRedisDataBase database, string key);
		ValueTask<(bool, string?)> TryGetStringAsync(IRedisDataBase database, string key);
	}

	public class RedisCacheService : IRedisCacheService
	{
		private readonly ILogger _logger = Log.ForContext<RedisCacheService>();

		private readonly IRedisCacheServiceConfig _config;

		public RedisCacheService(IRedisCacheServiceConfig config)
		{
			_config = config;
		}

		public IRedisDataBase Connect()
		{
			var connectionString = _config.ConnectionString;
			var result = new RedisDataBase(connectionString);
			_logger.Debug("Connected to {ConnectionString}", connectionString);
			return result;
		}

		public (bool, string?) TryGetString(IRedisDataBase database, string key)
		{
			if (!database.Database.KeyExists(key))
				return (false, default);

			var result = database.Database.StringGet(key);
			return (true, result);
		}

		public async ValueTask<(bool, string?)> TryGetStringAsync(IRedisDataBase database, string key)
		{
			if (!await database.Database.KeyExistsAsync(key))
				return (false, default);

			var result = await database.Database.StringGetAsync(key);
			return (true, result);
		}

		public (bool, T?) TryGet<T>(IRedisDataBase database, string key)
		{
			if (!database.Database.KeyExists(key))
				return (false, default);

			string? result = database.Database.StringGet(key);

			if (string.IsNullOrWhiteSpace(result))
				return (true, default);

			return (true, JsonConvert.DeserializeObject<T>(result));
		}

		public async ValueTask<(bool, T?)> TryGetAsync<T>(IRedisDataBase database, string key)
		{
			if (!await database.Database.KeyExistsAsync(key))
				return (false, default);

			string? result = await database.Database.StringGetAsync(key);

			if (string.IsNullOrWhiteSpace(result))
				return (true, default);

			return (true, JsonConvert.DeserializeObject<T>(result));
		}

		public void SetString(IRedisDataBase database, string key, string value, TimeSpan expiry)
		{
			database.Database.StringSet(key, value, expiry);
			_logger.Debug("Raw {Key} is saved for {Expiry}", key, expiry);
		}

		public async Task SetStringAsync(IRedisDataBase database, string key, string value, TimeSpan expiry)
		{
			await database.Database.StringSetAsync(key, value, expiry);
			_logger.Debug("Raw {Key} is saved for {Expiry}", key, expiry);
		}

		public void Set<T>(IRedisDataBase database, string key, T value, TimeSpan expiry)
		{
			var valueString = JsonConvert.SerializeObject(value);
			SetString(database, key, valueString, expiry);
		}

		public async Task SetAsync<T>(IRedisDataBase database, string key, T value, TimeSpan expiry)
		{
			var valueString = JsonConvert.SerializeObject(value);
			await SetStringAsync(database, key, valueString, expiry);
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}
	}

	public interface IRedisDataBase : IDisposable
	{
		IDatabase Database { get; }
	}

	public class RedisDataBase : IRedisDataBase
	{
		public IDatabase Database { get; } = default!;

		private readonly ConnectionMultiplexer _connectionMultiplexer;

		public RedisDataBase(string connectionString)
		{
			_connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
			Database = _connectionMultiplexer.GetDatabase();
		}

		public void Dispose()
		{
			_connectionMultiplexer?.Dispose();
		}

		~RedisDataBase()
		{
			Dispose();
		}
	}
}

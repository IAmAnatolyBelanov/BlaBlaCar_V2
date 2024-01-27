using Newtonsoft.Json;

using StackExchange.Redis;

namespace WebApi.Services.Redis
{
	public interface IRedisCacheService
	{
		void Clear();
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
		public ConnectionMultiplexer ConnectionMultiplexer { get; }
		public IDatabase Database { get; }

		private readonly RedisDataBaseFactory _source;

		public RedisDataBase(
			ConnectionMultiplexer connectionMultiplexer,
			IDatabase database,
			RedisDataBaseFactory source)
		{
			ConnectionMultiplexer = connectionMultiplexer;
			Database = database;

			_source = source;
		}

		public void Dispose()
		{
			_source.DisposeDataBase(this);
		}

		~RedisDataBase()
		{
			Dispose();
		}
	}

	public interface IRedisDataBaseFactory
	{
		IRedisDataBase Connect();
	}

	public class RedisDataBaseFactory : IRedisDataBaseFactory
	{
		private readonly Dictionary<ConnectionMultiplexer, (HashSet<RedisDataBase> HashSet, DateTimeOffset CreateDateTime)> _usages = [];
		private LastConnect _last;

		private readonly IRedisCacheServiceConfig _config;
		private readonly IClock _clock;
		private readonly ILogger _logger = Log.ForContext<RedisDataBaseFactory>();

		private readonly object _locker = new();

		public RedisDataBaseFactory(IRedisCacheServiceConfig config, IClock clock)
		{
			_config = config;
			_clock = clock;

			_ = StartDisposer();
		}

		public IRedisDataBase Connect()
		{
			var now = _clock.Now;
			var last = _last;

			if (CanConnect(last, now))
			{
				return ConnectAndRemember(last);
			}

			lock (_locker)
			{
				last = _last;
				if (CanConnect(last, now))
				{
					return ConnectAndRemember(last);
				}

				var connectionMultiplexer = ConnectionMultiplexer.Connect(_config.ConnectionString);
				var database = connectionMultiplexer.GetDatabase();
				var deadline = now + _config.ConnectionLifetime;

				_usages[connectionMultiplexer] = (new(), now);

				last = new(connectionMultiplexer, database, deadline);
				_last = last;
			}

			_logger.Information("Opened new connection to redis {ConnectionString}", _config.ConnectionString);
			return ConnectAndRemember(last);
		}

		public void DisposeDataBase(RedisDataBase redisDataBase)
		{
			_usages[redisDataBase.ConnectionMultiplexer].HashSet.Remove(redisDataBase);
		}

		private IRedisDataBase ConnectAndRemember(LastConnect last)
		{
			var result = new RedisDataBase(last.Multiplexer!, last.Database!, this);
			_usages[last.Multiplexer!].HashSet.Add(result);
			return result;
		}

		private bool CanConnect(LastConnect last, DateTimeOffset now)
		{
			return last.Database is not null && last.Deadline > now;
		}

		private async Task StartDisposer()
		{
			var shift = TimeSpan.FromSeconds(30);

			while (true)
			{
				await Task.Delay(30_000);

				if (_usages.Count < 2)
					continue;

				var last = _last;
				var now = _clock.Now;

				var usages = _usages.ToArray();

				for (int i = 0; i < usages.Length; i++)
				{
					var usage = usages[i];

					if (usage.Key == last.Multiplexer || usage.Value.CreateDateTime >= now - shift)
						continue;

					if (usage.Value.HashSet.Count == 0)
					{
						lock (_locker)
						{
							_usages.Remove(usage.Key);
						}
					}
				}
			}
		}

		private readonly record struct LastConnect(
			ConnectionMultiplexer? Multiplexer,
			IDatabase? Database,
			DateTimeOffset Deadline);
	}
}

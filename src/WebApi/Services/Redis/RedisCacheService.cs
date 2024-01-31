using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace WebApi.Services.Redis
{
	public interface IRedisCacheService
	{
		void Clear();
		void Set<T>(string key, T value, TimeSpan expiry);
		Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct);
		void SetString(string key, string value, TimeSpan expiry);
		Task SetStringAsync(string key, string value, TimeSpan expiry, CancellationToken ct);
		(bool, T?) TryGet<T>(string key);
		ValueTask<(bool, T?)> TryGetAsync<T>(string key, CancellationToken ct);
		(bool, string?) TryGetString(string key);
		ValueTask<(bool, string?)> TryGetStringAsync(string key, CancellationToken ct);
	}

	public class RedisCacheService : IRedisCacheService
	{
		private readonly ILogger _logger = Log.ForContext<RedisCacheService>();

		private readonly IRedisCacheServiceConfig _config;

		private readonly SemaphoreSlim _semaphore;
		private readonly object _locker = new();

		private IDatabase? _current;

		private readonly IAsyncPolicy _asyncPolicy;
		private readonly ISyncPolicy _syncPolicy;

		public RedisCacheService(IRedisCacheServiceConfig config)
		{
			_config = config;

			_semaphore = new SemaphoreSlim(_config.ConcurrentConnections, _config.ConcurrentConnections);

			var handleRedisExceptionsPolicy = Policy
				.Handle<RedisException>()
				.Or<RedisCommandException>()
				.Or<RedisTimeoutException>()
				.Or<RedisConnectionException>();

			var retryRedisAsyncPolicy = handleRedisExceptionsPolicy
				.WaitAndRetryAsync(_config.RetriesCount,
				attempt => _config.RetriesDelay + TimeSpan.FromMilliseconds(40 * attempt),
				(exception, timespan, attempt, context) =>
				{
					_logger.Error(exception, "Failed to execute redis command. Attempt {Attempt}, time delay {TimeDelay}, context {Context}",
						attempt, timespan, context);
				});
			var circuitBreakerAsyncPolicy = handleRedisExceptionsPolicy
				.CircuitBreakerAsync(_config.CircuitBreakerAllowedExceptionsCount, _config.CircuitBreakerOpenPeriod,
				onBreak: (exception, delay, context) =>
				{
					_logger.Error(exception, "Async redis circuit breaker is opened. Context: {Context}. Waiting {Delay} before next attempt", context, delay);
				},
				onReset: context => _logger.Information("Async redis circuit breaker is closed. Context {Context}", context));
			var retryCircuitBreakerAsyncPolixy = handleRedisExceptionsPolicy
				.Or<BrokenCircuitException>()
				.Or<IsolatedCircuitException>()
				.WaitAndRetryAsync(_config.CircuitBreakerRetriesCount,
				attempt => _config.CircuitBreakerOpenPeriod,
				(exception, timespan, attempt, context) =>
				{
					_logger.Error(exception, "Wait for async circuit breaker. Attempt {Attempt}, time delay {TimeDelay}, context {Context}",
						attempt, timespan, context);
				});
			_asyncPolicy = Policy.WrapAsync(retryRedisAsyncPolicy, circuitBreakerAsyncPolicy, retryCircuitBreakerAsyncPolixy);

			var retryRedisSyncPolicy = handleRedisExceptionsPolicy
				.WaitAndRetry(_config.RetriesCount,
				attempt => _config.RetriesDelay + TimeSpan.FromMilliseconds(40 * attempt),
				(exception, timespan, attempt, context) =>
				{
					_logger.Error(exception, "Failed to execute redis command. Attempt {Attempt}, time delay {TimeDelay}, context {Context}",
						attempt, timespan, context);
				});
			var circuitBreakerSyncPolicy = handleRedisExceptionsPolicy
				.CircuitBreaker(_config.CircuitBreakerAllowedExceptionsCount, _config.CircuitBreakerOpenPeriod,
				onBreak: (exception, delay, context) =>
				{
					_logger.Error(exception, "Sync redis circuit breaker is opened. Context: {Context}. Waiting {Delay} before next attempt", context, delay);
				},
				onReset: context => _logger.Information("Sync redis circuit breaker is closed. Context {Context}", context));
			var retryCircuitBreakerSyncPolixy = handleRedisExceptionsPolicy
				.Or<BrokenCircuitException>()
				.Or<IsolatedCircuitException>()
				.WaitAndRetry(_config.CircuitBreakerRetriesCount,
				attempt => _config.CircuitBreakerOpenPeriod,
				(exception, timespan, attempt, context) =>
				{
					_logger.Error(exception, "Wait for sync circuit breaker. Attempt {Attempt}, time delay {TimeDelay}, context {Context}",
						attempt, timespan, context);
				});
			_syncPolicy = Policy.Wrap(retryRedisSyncPolicy, circuitBreakerSyncPolicy, retryCircuitBreakerSyncPolixy);

			_ = PingForever();
		}

		public (bool, string?) TryGetString(string key)
		{
			return _syncPolicy.Execute(() =>
			{
				_semaphore.Wait();
				try
				{
					var connection = GetConnecntion();
					var keyExists = connection.KeyExists(key);

					if (!keyExists)
						return (false, default);

					string? result = connection.StringGet(key);
					return (true, result);
				}
				finally
				{
					_semaphore.Release();
				}
			});
		}

		public async ValueTask<(bool, string?)> TryGetStringAsync(string key, CancellationToken ct)
		{
			return await _asyncPolicy.ExecuteAsync(async (ctInternal) =>
			{
				await _semaphore.WaitAsync(ctInternal);
				try
				{
					var connection = GetConnecntion();
					var keyExists = await connection.KeyExistsAsync(key);

					if (!keyExists)
						return (false, default);

					var result = await connection.StringGetAsync(key);
					return (true, result);
				}
				finally
				{
					_semaphore.Release();
				}
			}, ct);
		}

		public (bool, T?) TryGet<T>(string key)
		{
			(var keyExists, var result) = TryGetString(key);

			if (!keyExists)
				return (false, default);

			if (string.IsNullOrWhiteSpace(result))
				return (true, default);

			return (true, JsonConvert.DeserializeObject<T>(result));
		}

		public async ValueTask<(bool, T?)> TryGetAsync<T>(string key, CancellationToken ct)
		{
			(var keyExists, var result) = await TryGetStringAsync(key, ct);

			if (!keyExists)
				return (false, default);

			if (string.IsNullOrWhiteSpace(result))
				return (true, default);

			return (true, JsonConvert.DeserializeObject<T>(result));
		}

		public void SetString(string key, string value, TimeSpan expiry)
		{
			_syncPolicy.Execute(() =>
			{
				_semaphore.Wait();
				try
				{
					var connection = GetConnecntion();
					connection.StringSet(key, value, expiry);
				}
				finally
				{
					_semaphore.Release();
				}
				_logger.Debug("Raw {Key} is saved for {Expiry}", key, expiry);
			});
		}

		public async Task SetStringAsync(string key, string value, TimeSpan expiry, CancellationToken ct)
		{
			await _asyncPolicy.ExecuteAsync(async ctInternal =>
			{
				await _semaphore.WaitAsync(ctInternal);
				try
				{
					var connection = GetConnecntion();
					await connection.StringSetAsync(key, value, expiry);
				}
				finally
				{
					_semaphore.Release();
				}
				_logger.Debug("Raw {Key} is saved for {Expiry}", key, expiry);
			}, ct);
		}

		public void Set<T>(string key, T value, TimeSpan expiry)
		{
			var valueString = JsonConvert.SerializeObject(value);
			SetString(key, valueString, expiry);
		}

		public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct)
		{
			var valueString = JsonConvert.SerializeObject(value);
			await SetStringAsync(key, valueString, expiry, ct);
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		private IDatabase GetConnecntion()
		{
			var current = _current;
			if (current is not null && current.Multiplexer.IsConnected)
				return current;

			lock (_locker)
			{
				current = _current;
				if (current is not null && current.Multiplexer.IsConnected)
					return current;

				if (current is not null)
					current.Multiplexer.Dispose();

				UpdateCurrentConnection();

				return _current!;
			}
		}

		private void UpdateCurrentConnection()
		{
			try
			{
				lock (_locker)
				{
					var current = _current;
					if (current is not null)
						current.Multiplexer.Dispose();

					var multiplexer = ConnectionMultiplexer.Connect(_config.ConnectionString);
					var db = multiplexer.GetDatabase();

					_current = db;
				}

				_logger.Information("Opened new connection to {RedisConnectionString}", _config.ConnectionString);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to open connection to {RedisConnectionString}", _config.ConnectionString);
				throw;
			}
		}

		private async Task PingForever()
		{
			while (true)
			{
				try
				{
					var connection = GetConnecntion();
					connection.Ping();
					_logger.Debug("Redis {Redis} was pinged successfully", _config.ConnectionString);
				}
				catch (Exception ex)
				{
					_logger.Error(ex, "Redis {Redis} ping failed. Try to reconnect", _config.ConnectionString);

					lock (_locker)
					{
						try
						{
							UpdateCurrentConnection();
							continue;
						}
						catch (Exception ex2)
						{
							_logger.Error(ex2, "Failed to open connection to {RedisConnectionString}", _config.ConnectionString);
						}
					}

					if (_config.ReconnectDelay != TimeSpan.Zero)
						await Task.Delay(_config.ReconnectDelay);

					continue;
				}

				await Task.Delay(_config.PingPeriod);
			}
		}
	}
}

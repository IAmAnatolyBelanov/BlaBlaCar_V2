using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using System.Diagnostics.CodeAnalysis;

namespace WebApi.Services.InMemoryCaches
{
	public interface IInMemoryCache<TKey, TValue>
		where TKey : class
	{
		TValue Set(TKey key, TValue value, TimeSpan absoluteExpirationRelativeToNow);
		bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value);
	}

	public class InMemoryCache<TKey, TValue> : IInMemoryCache<TKey, TValue>
		where TKey : class
	{
		private readonly IMemoryCache _memoryCache;
		private readonly ILogger _logger = Log.ForContext<InMemoryCache<TKey, TValue>>();

		public InMemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
		{
			_memoryCache = new MemoryCache(optionsAccessor);
		}

		public TValue Set(TKey key, TValue value, TimeSpan absoluteExpirationRelativeToNow)
		{
			using ICacheEntry entry = _memoryCache.CreateEntry(key);
			entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
			entry.Size = 1;
			entry.Value = value;

			_logger.Debug("For key {Key} value set in memory cache", key);

			return value;
		}

		public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
			=> _memoryCache.TryGetValue(key, out value);
	}
}

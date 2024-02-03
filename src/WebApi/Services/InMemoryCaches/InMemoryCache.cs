using Microsoft.Extensions.Caching.Memory;
using Riok.Mapperly.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace WebApi.Services.InMemoryCaches
{
	public interface IInMemoryCache<TValue> : IInMemoryCache<string, TValue>
	{
	}

	public interface IInMemoryCache<TKey, TValue>
		where TKey : class
	{
		TValue Set(TKey key, TValue value, TimeSpan absoluteExpirationRelativeToNow);
		bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value);
	}

	public class InMemoryCache<TValue> : InMemoryCache<string, TValue>, IInMemoryCache<TValue>
	{
		public InMemoryCache(IInMemoryCacheConfigMapper mapper, IInMemoryCacheConfig config)
			: base(mapper, config)
		{
		}
	}

	public class InMemoryCache<TKey, TValue> : IInMemoryCache<TKey, TValue>
		where TKey : class
	{
		private readonly IMemoryCache _memoryCache;
		private readonly ILogger _logger = Log.ForContext<InMemoryCache<TKey, TValue>>();
		private readonly IInMemoryCacheConfigMapper _mapper;

		public InMemoryCache(IInMemoryCacheConfigMapper mapper, IInMemoryCacheConfig config)
		{
			_mapper = mapper;
			_memoryCache = new MemoryCache(_mapper.ToCacheOptions(config));
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

	public interface IInMemoryCacheConfig : IValidatableConfig
	{
		/// <summary>
		/// Gets the amount to compact the cache by when the maximum size is exceeded.
		/// </summary>
		public double CompactionPercentage { get; }

		/// <summary>
		/// Gets the minimum length of time between successive scans for expired items.
		/// </summary>
		public TimeSpan ExpirationScanFrequency { get; }

		/// <summary>
		/// Gets the maximum size of the cache.
		/// </summary>
		public long SizeLimit { get; }

		/// <summary>
		/// Gets whether to track linked entries. Disabled by default.
		/// </summary>
		public bool TrackLinkedCacheEntries { get; }

		/// <summary>
		/// Gets whether to track memory cache statistics. Disabled by default.
		/// </summary>
		public bool TrackStatistics { get; }
	}

	public class InMemoryCacheConfig : IInMemoryCacheConfig
	{
		/// <inheritdoc/>
		public double CompactionPercentage { get; set; }
		/// <inheritdoc/>
		public TimeSpan ExpirationScanFrequency { get; set; }
		/// <inheritdoc/>
		public long SizeLimit { get; set; } = 100_000;
		/// <inheritdoc/>
		public bool TrackLinkedCacheEntries { get; set; }
		/// <inheritdoc/>
		public bool TrackStatistics { get; set; }

		public IEnumerable<string> GetValidationErrors()
		{
			if (SizeLimit <= 0)
				yield return $"{nameof(SizeLimit)} must be >= 1";

			if (ExpirationScanFrequency < TimeSpan.Zero)
				yield return $"{nameof(ExpirationScanFrequency)} must be >= 0";

			if (CompactionPercentage < 0)
				yield return $"{nameof(CompactionPercentage)} must be >= 0";
		}
	}

	public interface IInMemoryCacheConfigMapper
	{
		MemoryCacheOptions ToCacheOptions(IInMemoryCacheConfig config);
	}

	[Mapper]
	public partial class InMemoryCacheConfigMapper : IInMemoryCacheConfigMapper
	{
		public partial MemoryCacheOptions ToCacheOptions(IInMemoryCacheConfig config);
	}
}

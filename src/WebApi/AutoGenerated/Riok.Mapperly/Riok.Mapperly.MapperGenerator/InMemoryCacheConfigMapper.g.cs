﻿// <auto-generated />
#nullable enable
namespace WebApi.Services.InMemoryCaches
{
    public partial class InMemoryCacheConfigMapper
    {
        public partial global::Microsoft.Extensions.Caching.Memory.MemoryCacheOptions ToCacheOptions(global::WebApi.Services.InMemoryCaches.IInMemoryCacheConfig config)
        {
            var target = new global::Microsoft.Extensions.Caching.Memory.MemoryCacheOptions();
            target.CompactionPercentage = config.CompactionPercentage;
            target.ExpirationScanFrequency = config.ExpirationScanFrequency;
            target.SizeLimit = config.SizeLimit;
            target.TrackLinkedCacheEntries = config.TrackLinkedCacheEntries;
            target.TrackStatistics = config.TrackStatistics;
            return target;
        }
    }
}
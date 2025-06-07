using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace DynamoDBProcessor.Services;

/// <summary>
/// Implementation of ICacheService using IMemoryCache for in-memory caching.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private const int MaxCacheSize = 1000; // Maximum number of items in cache

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_cache.TryGetValue(key, out string? cachedValue))
            {
                if (cachedValue == null)
                {
                    return Task.FromResult<T?>(default);
                }

                var value = JsonSerializer.Deserialize<T>(cachedValue);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from cache for key: {Key}", key);
            throw new DynamoDBProcessorException(
                "Failed to retrieve value from cache",
                "CACHE_ERROR",
                "CacheError",
                ex);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var options = new MemoryCacheEntryOptions()
                .SetSize(1) // Each item has size 1
                .SetPriority(CacheItemPriority.Normal);

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }

            var serializedValue = JsonSerializer.Serialize(value);
            _cache.Set(key, serializedValue, options);
            _logger.LogDebug("Value cached for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
            throw new DynamoDBProcessorException(
                "Failed to set value in cache",
                "CACHE_ERROR",
                "CacheError",
                ex);
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            _cache.Remove(key);
            _logger.LogDebug("Value removed from cache for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
            throw new DynamoDBProcessorException(
                "Failed to remove value from cache",
                "CACHE_ERROR",
                "CacheError",
                ex);
        }
    }
} 
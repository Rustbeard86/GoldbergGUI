using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GoldbergGUI.Core.Services;

/// <summary>
///     Service for caching Steam app data in memory
/// </summary>
public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    void Remove(string key);
    void Clear();
}

/// <summary>
///     Implementation of memory caching service with automatic cache invalidation
/// </summary>
public sealed class CacheService(IMemoryCache cache, ILogger<CacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(1);

    /// <summary>
    ///     Gets cached value or creates it using the factory function
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
    {
        if (cache.TryGetValue<T>(key, out var cachedValue))
        {
            logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        logger.LogDebug("Cache miss for key: {Key}, creating value", key);

        var value = await factory().ConfigureAwait(false);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(absoluteExpiration ?? DefaultExpiration)
            .SetSize(1); // Each entry counts as 1 unit

        cache.Set(key, value, cacheEntryOptions);

        logger.LogDebug("Cached value for key: {Key}", key);

        return value;
    }

    /// <summary>
    ///     Removes a specific cache entry
    /// </summary>
    public void Remove(string key)
    {
        cache.Remove(key);
        logger.LogDebug("Removed cache entry for key: {Key}", key);
    }

    /// <summary>
    ///     Clears all cache entries
    /// </summary>
    public void Clear()
    {
        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove 100% of entries
            logger.LogInformation("Cache cleared");
        }
    }
}
using System.Collections.Concurrent;
using System.Text.Json;
using Demif.Application.Abstractions.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// Redis distributed cache implementation using IDistributedCache
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly ConcurrentDictionary<string, bool> _cacheKeys = new();

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, absoluteExpirationRelativeToNow, cancellationToken);
        }

        return value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedBytes = await _cache.GetAsync(key, cancellationToken);
            if (cachedBytes is null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache for key: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1)
            };

            var serializedData = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, serializedData, options, cancellationToken);

            _cacheKeys.TryAdd(key, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache for key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _cacheKeys.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache for key: {CacheKey}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = _cacheKeys.Keys.Where(k => k.StartsWith(prefixKey)).ToList();
            var tasks = keysToRemove.Select(k => RemoveAsync(k, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache by prefix: {PrefixKey}", prefixKey);
        }
    }
}

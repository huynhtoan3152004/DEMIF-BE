namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Interface for Distributed Cache Operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets data from cache or creates it using the factory function if it doesn't exist
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets data into cache
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes data from cache by exact key
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cache entries that start with the given prefix
    /// Note: Depending on implementation, this might be expensive or unsupported for some providers.
    /// </summary>
    Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);
}

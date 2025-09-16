using System.Collections.Concurrent;

namespace WimGetInfoApi.Services.Caching
{
    /// <summary>
    /// SRP: Responsible only for caching WIM properties
    /// </summary>
    public interface IWimPropertyCache
    {
        /// <summary>
        /// Gets properties asynchronously by cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Dictionary of properties or null if not found</returns>
        Task<Dictionary<string, object>?> GetAsync(string key);
        
        /// <summary>
        /// Sets properties asynchronously by cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="properties">Properties to cache</param>
        Task SetAsync(string key, Dictionary<string, object> properties);
        
        /// <summary>
        /// Clears all cached properties
        /// </summary>
        void ClearCache();
        
        // Legacy methods for backward compatibility
        /// <summary>
        /// Gets a property value by cache key (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of property</typeparam>
        /// <param name="cacheKey">Cache key</param>
        /// <returns>Property value or null</returns>
        T? GetProperty<T>(string cacheKey) where T : class;
        
        /// <summary>
        /// Sets a property value by cache key (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of property</typeparam>
        /// <param name="cacheKey">Cache key</param>
        /// <param name="value">Property value</param>
        void SetProperty<T>(string cacheKey, T value) where T : class;
        
        /// <summary>
        /// Removes all cache entries for a specific file
        /// </summary>
        /// <param name="filePath">File path to remove from cache</param>
        void RemoveFile(string filePath);
    }

    /// <summary>
    /// Implementation of WIM property caching following Single Responsibility Principle
    /// </summary>
    public class WimPropertyCache : IWimPropertyCache
    {
        private readonly ConcurrentDictionary<string, object> _cache;
        private readonly ILogger<WimPropertyCache> _logger;

        /// <summary>
        /// Initializes a new instance of the WimPropertyCache
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public WimPropertyCache(ILogger<WimPropertyCache> logger)
        {
            _cache = new ConcurrentDictionary<string, object>();
            _logger = logger;
        }

        /// <summary>
        /// Gets properties asynchronously by cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Dictionary of properties or null if not found</returns>
        public async Task<Dictionary<string, object>?> GetAsync(string key)
        {
            return await Task.FromResult(GetProperty<Dictionary<string, object>>(key));
        }

        /// <summary>
        /// Sets properties asynchronously by cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="properties">Properties to cache</param>
        public async Task SetAsync(string key, Dictionary<string, object> properties)
        {
            await Task.Run(() => SetProperty(key, properties));
        }

        /// <summary>
        /// Clears all cached properties
        /// </summary>
        public void ClearCache()
        {
            Clear();
        }

        /// <summary>
        /// Gets a property value by cache key (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of property</typeparam>
        /// <param name="cacheKey">Cache key</param>
        /// <returns>Property value or null</returns>
        public T? GetProperty<T>(string cacheKey) where T : class
        {
            if (_cache.TryGetValue(cacheKey, out var value))
            {
                _logger.LogTrace("Cache hit for key: {CacheKey}", cacheKey);
                return value as T;
            }

            _logger.LogTrace("Cache miss for key: {CacheKey}", cacheKey);
            return null;
        }

        /// <summary>
        /// Sets a property value by cache key (legacy method)
        /// </summary>
        /// <typeparam name="T">Type of property</typeparam>
        /// <param name="cacheKey">Cache key</param>
        /// <param name="value">Property value</param>
        public void SetProperty<T>(string cacheKey, T value) where T : class
        {
            _cache.TryAdd(cacheKey, value);
            _logger.LogTrace("Cached property for key: {CacheKey}", cacheKey);
        }

        /// <summary>
        /// Clears the cache (legacy method)
        /// </summary>
        public void Clear()
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogDebug("Cache cleared. Removed {Count} items", count);
        }

        /// <summary>
        /// Removes all cache entries for a specific file
        /// </summary>
        /// <param name="filePath">File path to remove from cache</param>
        public void RemoveFile(string filePath)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith($"{filePath}:")).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
            _logger.LogDebug("Removed {Count} cache entries for file: {FilePath}", keysToRemove.Count, filePath);
        }
    }
}

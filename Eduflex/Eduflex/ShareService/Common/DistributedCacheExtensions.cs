using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace ShareService.Common
{
    public static class DistributedCacheExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key)
        {
            var bytes = await cache.GetAsync(key);
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }

        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, TimeSpan expiration)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
            await cache.SetAsync(key, bytes, options);
        }
    }
}
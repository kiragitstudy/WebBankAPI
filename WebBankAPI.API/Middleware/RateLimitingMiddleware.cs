using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace WebBankAPI.API.Middleware;

public class RateLimitingMiddleware(
    RequestDelegate next,
    IDistributedCache cache,
    ILogger<RateLimitingMiddleware> logger)
{
    private const int MaxRequests = 100;
    private const int TimeWindowSeconds = 60;

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var cacheKey = $"rate_limit_{clientId}";

        var requestCount = await GetRequestCountAsync(cacheKey);

        if (requestCount >= MaxRequests)
        {
            logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Rate limit exceeded. Please try again later.",
                errors = new[] { "Too many requests" }
            });
            return;
        }

        await IncrementRequestCountAsync(cacheKey);
        await next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
            return userId;

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return ipAddress ?? "unknown";
    }

    private async Task<int> GetRequestCountAsync(string key)
    {
        var value = await cache.GetStringAsync(key);
        return int.TryParse(value, out var count) ? count : 0;
    }

    private async Task IncrementRequestCountAsync(string key)
    {
        var currentCount = await GetRequestCountAsync(key);
        var newCount = currentCount + 1;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(TimeWindowSeconds)
        };

        await cache.SetStringAsync(key, newCount.ToString(), options);
    }
}

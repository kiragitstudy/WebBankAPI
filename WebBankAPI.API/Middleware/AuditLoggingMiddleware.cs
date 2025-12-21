using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebBankAPI.Domain.Entities;
using WebBankAPI.Infrastructure.Data;

namespace WebBankAPI.API.Middleware;

public class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, BankDbContext dbContext)
    {
        // Only audit specific endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (ShouldAudit(path))
        {
            var userId = GetUserId(context);
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"{context.Request.Method} {context.Request.Path}",
                EntityType = ExtractEntityType(path),
                EntityId = ExtractEntityId(path),
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Timestamp = DateTime.UtcNow
            };

            try
            {
                dbContext.AuditLogs.Add(auditLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write audit log");
            }
        }

        await next(context);
    }

    private static bool ShouldAudit(string path)
    {
        return path.Contains("/transactions") || 
               path.Contains("/accounts") || 
               path.Contains("/auth/login");
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string ExtractEntityType(string path)
    {
        if (path.Contains("transaction")) return "Transaction";
        if (path.Contains("account")) return "Account";
        if (path.Contains("auth")) return "Auth";
        return "Unknown";
    }

    private static string ExtractEntityId(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 2 && Guid.TryParse(segments[^1], out _) 
            ? segments[^1] 
            : "N/A";
    }
}
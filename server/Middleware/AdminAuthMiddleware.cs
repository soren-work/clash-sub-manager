using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ClashSubManager.Middleware
{
    public class AdminAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _hmacKey;
        private readonly ILogger<AdminAuthMiddleware> _logger;

        public AdminAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AdminAuthMiddleware> logger)
        {
            _next = next;
            _hmacKey = configuration["CookieSecretKey"] ?? "default-key";
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request?.Path.Value?.ToLowerInvariant();
            var remoteIp = context.Connection?.RemoteIpAddress?.ToString();
            
            if (path?.StartsWith("/admin") == true)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Admin request received. Path: {Path}, RemoteIp: {RemoteIp}",
                        context.Request?.Path.Value,
                        remoteIp);
                }

                if (path == "/admin/login" || path == "/admin/logout")
                {
                    await _next(context);
                    return;
                }

                var sessionCookie = context.Request?.Cookies?["AdminSession"];
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    _logger.LogInformation(
                        "Admin session cookie missing. Path: {Path}, RemoteIp: {RemoteIp}",
                        context.Request?.Path.Value,
                        remoteIp);

                    context.Response.Redirect("/Admin/Login");
                    return;
                }

                if (!TryValidateSessionCookie(sessionCookie, out var failureReason))
                {
                    _logger.LogWarning(
                        "Admin session cookie validation failed. Reason: {Reason}, Path: {Path}, RemoteIp: {RemoteIp}",
                        failureReason,
                        context.Request?.Path.Value,
                        remoteIp);

                    context.Response.Redirect("/Admin/Login");
                    return;
                }
            }

            await _next(context);
        }
        
        /// <summary>
        /// Validates the session cookie format, expiration, and HMAC signature
        /// Cookie format: sessionId:timestamp:signature
        /// </summary>
        /// <param name="cookieValue">The session cookie value</param>
        /// <param name="failureReason">The reason for validation failure</param>
        /// <returns>True if the cookie is valid and not expired</returns>
        private bool TryValidateSessionCookie(string cookieValue, out string failureReason)
        {
            failureReason = string.Empty;

            if (string.IsNullOrEmpty(cookieValue))
            {
                failureReason = "EmptyCookie";
                return false;
            }
            
            var parts = cookieValue.Split(':');
            if (parts.Length != 3)
            {
                failureReason = "InvalidFormat";
                return false;
            }
            
            var sessionId = parts[0];
            var timestampStr = parts[1];
            var signature = parts[2];
            
            // Validate sessionId format (GUID)
            if (!Guid.TryParseExact(sessionId, "N", out _))
            {
                failureReason = "InvalidSessionId";
                return false;
            }
            
            // Parse and validate expiration time
            if (!DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var expiresAt))
            {
                failureReason = "InvalidTimestamp";
                return false;
            }
            
            // Check if session has expired
            if (DateTime.UtcNow > expiresAt)
            {
                failureReason = "Expired";
                return false;
            }
            
            // Validate HMAC signature to prevent tampering
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            var signatureData = $"{sessionId}|{timestampStr}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            var expectedSignature = Convert.ToBase64String(hash);
            
            if (signature != expectedSignature)
            {
                failureReason = "InvalidSignature";
                return false;
            }

            return true;
        }
    }

    public static class AdminAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseAdminAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AdminAuthMiddleware>();
        }
    }
}
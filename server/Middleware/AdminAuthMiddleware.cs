using System.Security.Cryptography;
using System.Text;

namespace ClashSubManager.Middleware
{
    public class AdminAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _hmacKey;

        public AdminAuthMiddleware(RequestDelegate next)
        {
            _next = next;
            _hmacKey = Environment.GetEnvironmentVariable("COOKIE_SECRET_KEY") ?? "default-key";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            
            if (path?.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (path.Equals("/admin/login", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/admin/logout", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                var sessionCookie = context.Request.Cookies["AdminSession"];
                if (string.IsNullOrEmpty(sessionCookie) || !ValidateSessionCookie(sessionCookie))
                {
                    context.Response.Redirect("/admin/login");
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
        /// <returns>True if the cookie is valid and not expired</returns>
        private bool ValidateSessionCookie(string cookieValue)
        {
            if (string.IsNullOrEmpty(cookieValue)) return false;
            
            var parts = cookieValue.Split(':');
            if (parts.Length != 3) return false;
            
            var sessionId = parts[0];
            var timestampStr = parts[1];
            var signature = parts[2];
            
            // Validate sessionId format (GUID)
            if (!Guid.TryParseExact(sessionId, "N", out _))
            {
                return false;
            }
            
            // Parse and validate expiration time
            if (!DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var expiresAt))
            {
                return false;
            }
            
            // Check if session has expired
            if (DateTime.UtcNow > expiresAt)
            {
                return false;
            }
            
            // Validate HMAC signature to prevent tampering
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            var signatureData = $"{sessionId}|{timestampStr}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            var expectedSignature = Convert.ToBase64String(hash);
            
            return signature == expectedSignature;
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
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
                if (!ValidateSessionCookie(sessionCookie))
                {
                    context.Response.Redirect("/admin/login");
                    return;
                }
            }

            await _next(context);
        }
        
        private bool ValidateSessionCookie(string cookieValue)
        {
            if (string.IsNullOrEmpty(cookieValue)) return false;
            
            var parts = cookieValue.Split(':');
            if (parts.Length != 2) return false;
            
            var sessionId = parts[0];
            var signature = parts[1];
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            var signatureData = $"{sessionId}|{DateTime.UtcNow:yyyyMMddHHmmss}";
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
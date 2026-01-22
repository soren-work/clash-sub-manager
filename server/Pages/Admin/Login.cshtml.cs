using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly IStringLocalizer<LoginModel> _localizer;

        [BindProperty(SupportsGet = false)]
        public string? Username { get; set; }
        
        [BindProperty(SupportsGet = false)]
        public string? Password { get; set; }
        
        public string? ErrorMessage { get; set; }

        public LoginModel(IStringLocalizer<LoginModel> localizer)
        {
            _localizer = localizer;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = _localizer["UsernameAndPasswordRequired"];
                return Page();
            }

            if (!ValidateCredentials(Username, Password))
            {
                ErrorMessage = _localizer["InvalidCredentials"];
                return Page();
            }

            SetAuthCookie();
            return RedirectToPage("/Admin/Index");
        }
        
        private bool ValidateCredentials(string username, string password)
        {
            var configUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
            var configPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            return username == configUsername && password == configPassword;
        }
        
        private void SetAuthCookie()
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var timeoutMinutes = int.Parse(Environment.GetEnvironmentVariable("SESSION_TIMEOUT_MINUTES") ?? "30");
            var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
            
            var hmacKey = Environment.GetEnvironmentVariable("COOKIE_SECRET_KEY") ?? "default-key";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacKey));
            var signatureData = $"{sessionId}|{expiresAt:yyyyMMddHHmmss}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            var signature = Convert.ToBase64String(hash);
            
            var cookieValue = $"{sessionId}:{signature}";
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/admin"
            };
            
            Response.Cookies.Append("AdminSession", cookieValue, cookieOptions);
        }
    }
}

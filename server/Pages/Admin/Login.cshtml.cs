using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<SharedResources> _localizer;

        [BindProperty(SupportsGet = false)]
        public string? Username { get; set; }
        
        [BindProperty(SupportsGet = false)]
        public string? Password { get; set; }
        
        public string? ErrorMessage { get; set; }

        public LoginModel(IConfiguration configuration, IStringLocalizer<SharedResources> localizer)
        {
            _configuration = configuration;
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
            var configUsername = _configuration["AdminUsername"];
            var configPassword = _configuration["AdminPassword"];
            return username == configUsername && password == configPassword;
        }
        
        private void SetAuthCookie()
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var timeoutMinutes = int.Parse(_configuration["SessionTimeoutMinutes"] ?? "30");
            var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
            
            var hmacKey = _configuration["CookieSecretKey"] ?? "default-key";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacKey));
            var signatureData = $"{sessionId}|{expiresAt:yyyyMMddHHmmss}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            var signature = Convert.ToBase64String(hash);
            
            // Include timestamp in cookie for proper validation
            var cookieValue = $"{sessionId}:{expiresAt:yyyyMMddHHmmss}:{signature}";
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/"
            };
            
            Response.Cookies.Append("AdminSession", cookieValue, cookieOptions);
        }
    }
}

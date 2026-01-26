using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ClashSubManager.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(IStringLocalizer<SharedResources> localizer, ILogger<LogoutModel> logger)
        {
            _localizer = localizer;
            _logger = logger;
        }

        public IActionResult OnPost()
        {
            Response.Cookies.Delete("AdminSession");
            var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();
            _logger.LogInformation("Admin logout executed. RemoteIp: {RemoteIp}", remoteIp);
            return RedirectToPage("/Admin/Login");
        }
        
        public void OnGet()
        {
            OnPost();
        }
    }
}

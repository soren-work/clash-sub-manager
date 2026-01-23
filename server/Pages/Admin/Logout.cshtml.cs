using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ClashSubManager.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        private readonly IStringLocalizer<LogoutModel> _localizer;

        public LogoutModel(IStringLocalizer<LogoutModel> localizer)
        {
            _localizer = localizer;
        }

        public IActionResult OnPost()
        {
            Response.Cookies.Delete("AdminSession");
            return RedirectToPage("/Admin/Login");
        }
        
        public void OnGet()
        {
            OnPost();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ClashSubManager.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LogoutModel(IStringLocalizer<SharedResources> localizer)
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

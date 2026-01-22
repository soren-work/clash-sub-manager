using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ClashSubManager.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly IStringLocalizer<LoginModel> _localizer;

        public LoginModel(IStringLocalizer<LoginModel> localizer)
        {
            _localizer = localizer;
        }

        public void OnGet()
        {
        }

        public void OnPost()
        {
        }
    }
}

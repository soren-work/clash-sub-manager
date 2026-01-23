using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ClashSubManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IStringLocalizer<IndexModel> _localizer;

        public IndexModel(IStringLocalizer<IndexModel> localizer)
        {
            _localizer = localizer;
        }

        public void OnGet()
        {
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using ClashSubManager.Services;
using ClashSubManager.Models;

namespace ClashSubManager.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IUserManagementService _userManagementService;
        private readonly IConfigurationService _configurationService;
        private readonly FileService _fileService;

        public List<IPRecord> GlobalIPRecords { get; set; } = new();
        public string GlobalYAMLContent { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();
        public Dictionary<string, bool> UserConfigStatus { get; set; } = new();

        public IndexModel(
            IStringLocalizer<SharedResources> localizer,
            IUserManagementService userManagementService,
            IConfigurationService configurationService,
            FileService fileService)
        {
            _localizer = localizer;
            _userManagementService = userManagementService;
            _configurationService = configurationService;
            _fileService = fileService;
        }

        public async Task OnGetAsync()
        {
            await LoadPreviewData();
        }

        private async Task LoadPreviewData()
        {
            try
            {
                // 获取全局IP记录
                var globalIPPath = Path.Combine(_configurationService.GetDataPath(), "cloudflare-ip.csv");
                if (System.IO.File.Exists(globalIPPath))
                {
                    GlobalIPRecords = await _fileService.LoadDefaultIPsAsync();
                }

                // 获取全局YAML内容
                var globalYAMLPath = Path.Combine(_configurationService.GetDataPath(), "clash.yaml");
                if (System.IO.File.Exists(globalYAMLPath))
                {
                    GlobalYAMLContent = await System.IO.File.ReadAllTextAsync(globalYAMLPath);
                }

                // 获取用户列表和配置状态
                Users = await _userManagementService.GetAllUsersAsync();
                
                foreach (var user in Users)
                {
                    var userDir = Path.Combine(_configurationService.GetDataPath(), user);
                    UserConfigStatus[user] = System.IO.Directory.Exists(userDir);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止页面加载
                Console.WriteLine($"Error loading preview data: {ex.Message}");
            }
        }
    }
}

using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    public class UserConfigModel : PageModel
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IConfigurationService _configurationService;
        private readonly Services.FileService _fileService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }

        public List<string> AvailableUsers { get; set; } = new();
        public UserConfigurationInfo UserConfig { get; set; } = new();
        public List<IPRecord> IPRecords { get; set; } = new();
        public string YAMLContent { get; set; } = string.Empty;

        public UserConfigModel(
            IUserManagementService userManagementService,
            IConfigurationService configurationService,
            Services.FileService fileService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManagementService = userManagementService;
            _configurationService = configurationService;
            _fileService = fileService;
            _localizer = localizer;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUserListAsync();
            await LoadUserConfigurationAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteUserConfigAsync()
        {
            if (string.IsNullOrEmpty(SelectedUserId))
            {
                ModelState.AddModelError(string.Empty, _localizer["PleaseSelectUser"]);
                await LoadUserListAsync();
                return Page();
            }

            var result = await _userManagementService.DeleteUserAsync(SelectedUserId);
            
            if (result)
            {
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["UserConfigurationDeletedSuccessfully"].ToString();
                }
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, _localizer["FailedToDeleteUserConfiguration"]);
            await LoadUserListAsync();
            await LoadUserConfigurationAsync();
            return Page();
        }

        private async Task LoadUserListAsync()
        {
            try
            {
                AvailableUsers = await _userManagementService.GetAllUsersAsync();
            }
            catch
            {
                AvailableUsers = new List<string>();
            }
        }

        private async Task LoadUserConfigurationAsync()
        {
            if (string.IsNullOrEmpty(SelectedUserId))
            {
                UserConfig = new UserConfigurationInfo();
                return;
            }

            try
            {
                var dataPath = _configurationService.GetDataPath();
                var userDir = Path.Combine(dataPath, SelectedUserId);
                if (!Directory.Exists(userDir))
                {
                    UserConfig = new UserConfigurationInfo
                    {
                        UserId = SelectedUserId,
                        HasIPConfiguration = false,
                        HasTemplate = false,
                        DirectoryExists = false
                    };
                    return;
                }

                var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
                var templateFile = Path.Combine(userDir, "clash.yaml");

                UserConfig = new UserConfigurationInfo
                {
                    UserId = SelectedUserId,
                    DirectoryExists = true,
                    HasIPConfiguration = System.IO.File.Exists(ipFile),
                    HasTemplate = System.IO.File.Exists(templateFile),
                    IPFilePath = ipFile,
                    TemplateFilePath = templateFile
                };

                // Load IP file info
                if (UserConfig.HasIPConfiguration)
                {
                    var ipFileInfo = new System.IO.FileInfo(ipFile);
                    UserConfig.IPFileSize = ipFileInfo.Length;
                    UserConfig.IPFileLastModified = ipFileInfo.LastWriteTime;

                    // Load IP records for preview
                    IPRecords = await _fileService.LoadUserDedicatedIPsAsync(SelectedUserId);
                    
                    var ipContent = await System.IO.File.ReadAllTextAsync(ipFile, Encoding.UTF8);
                    UserConfig.IPCount = ipContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count();
                }

                // Load template file info
                if (UserConfig.HasTemplate)
                {
                    var templateFileInfo = new System.IO.FileInfo(templateFile);
                    UserConfig.TemplateFileSize = templateFileInfo.Length;
                    UserConfig.TemplateFileLastModified = templateFileInfo.LastWriteTime;
                    
                    // Load YAML content for preview
                    YAMLContent = await System.IO.File.ReadAllTextAsync(templateFile, Encoding.UTF8);
                }
            }
            catch
            {
                UserConfig = new UserConfigurationInfo { UserId = SelectedUserId };
            }
        }
    }

    public class UserConfigurationInfo
    {
        public string UserId { get; set; } = string.Empty;
        public bool DirectoryExists { get; set; }
        public bool HasIPConfiguration { get; set; }
        public bool HasTemplate { get; set; }
        public string IPFilePath { get; set; } = string.Empty;
        public string TemplateFilePath { get; set; } = string.Empty;
        public long IPFileSize { get; set; }
        public long TemplateFileSize { get; set; }
        public DateTime IPFileLastModified { get; set; }
        public DateTime TemplateFileLastModified { get; set; }
        public int IPCount { get; set; }
    }
}

using ClashSubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserConfigModel : PageModel
    {
        private readonly string _basePath = "/app/data";

        [BindProperty(SupportsGet = true)]
        public string SelectedUserId { get; set; }

        public List<string> AvailableUsers { get; set; } = new();
        public UserConfigurationInfo UserConfig { get; set; } = new();

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
                ModelState.AddModelError(string.Empty, "Please select a user");
                await LoadUserListAsync();
                return Page();
            }

            var result = await DeleteUserConfigurationAsync(SelectedUserId);
            
            if (result)
            {
                if (TempData != null)
                {
                    TempData["Success"] = "User configuration deleted successfully";
                }
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to delete user configuration");
            await LoadUserListAsync();
            await LoadUserConfigurationAsync();
            return Page();
        }

        private async Task LoadUserListAsync()
        {
            try
            {
                var usersPath = Path.Combine(_basePath, "users.txt");
                if (System.IO.File.Exists(usersPath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(usersPath, Encoding.UTF8);
                    AvailableUsers = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(line => line.Trim())
                                        .Where(line => !string.IsNullOrEmpty(line))
                                        .ToList();
                }
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
                var userDir = Path.Combine(_basePath, SelectedUserId);
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

                    var ipContent = await System.IO.File.ReadAllTextAsync(ipFile, Encoding.UTF8);
                    UserConfig.IPCount = ipContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count();
                }

                // Load template file info
                if (UserConfig.HasTemplate)
                {
                    var templateFileInfo = new System.IO.FileInfo(templateFile);
                    UserConfig.TemplateFileSize = templateFileInfo.Length;
                    UserConfig.TemplateFileLastModified = templateFileInfo.LastWriteTime;
                }
            }
            catch
            {
                UserConfig = new UserConfigurationInfo { UserId = SelectedUserId };
            }
        }

        private async Task<bool> DeleteUserConfigurationAsync(string userId)
        {
            try
            {
                var userDir = Path.Combine(_basePath, userId);
                if (System.IO.Directory.Exists(userDir))
                {
                    System.IO.Directory.Delete(userDir, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserConfigurationInfo
    {
        public string UserId { get; set; }
        public bool DirectoryExists { get; set; }
        public bool HasIPConfiguration { get; set; }
        public bool HasTemplate { get; set; }
        public string IPFilePath { get; set; }
        public string TemplateFilePath { get; set; }
        public long IPFileSize { get; set; }
        public long TemplateFileSize { get; set; }
        public DateTime IPFileLastModified { get; set; }
        public DateTime TemplateFileLastModified { get; set; }
        public int IPCount { get; set; }
    }
}

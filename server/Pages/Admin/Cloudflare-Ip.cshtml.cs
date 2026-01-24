using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    public class CloudflareIpModel : PageModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly CloudflareIPParserService _ipParserService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<CloudflareIpModel> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }

        public List<IPRecord> IPRecords { get; set; } = new();
        public List<string> AvailableUsers { get; set; } = new();
        public bool FileExists { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "CSVContentRequired")]
        public string CSVContent { get; set; } = string.Empty;

        public string OriginalCSVContent { get; set; } = string.Empty;

        public CloudflareIpModel(
            IConfigurationService configurationService,
            CloudflareIPParserService ipParserService,
            IStringLocalizer<SharedResources> localizer,
            ILogger<CloudflareIpModel> logger)
        {
            _configurationService = configurationService;
            _ipParserService = ipParserService;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSetIPsAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUserListAsync();
                await LoadIPRecordsAsync();
                return Page();
            }

            var result = await SetIPsAsync(CSVContent, SelectedUserId);

            if (result)
            {
                // Clear any existing validation errors
                ModelState.Clear();
                
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["IPConfigurationSaved"].ToString();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer["FailedToSaveIPConfiguration"]);
            }

            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, _localizer["PleaseSelectFileToUpload"]);
                await LoadUserListAsync();
                await LoadIPRecordsAsync();
                return Page();
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                ModelState.AddModelError(string.Empty, _localizer["FileSizeExceedsLimit"]);
                await LoadUserListAsync();
                await LoadIPRecordsAsync();
                return Page();
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            var result = await SetIPsAsync(content, SelectedUserId);

            if (result)
            {
                // Clear any existing validation errors
                ModelState.Clear();
                
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["FileUploadedSuccessfully"].ToString();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer["FailedToUploadFile"]);
            }

            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteIPsAsync()
        {
            var result = await DeleteIPsAsync(SelectedUserId);

            if (result)
            {
                // Clear any existing validation errors
                ModelState.Clear();
                
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["ConfigurationDeletedSuccessfully"].ToString();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer["FailedToDeleteConfiguration"]);
            }

            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        private async Task LoadUserListAsync()
        {
            try
            {
                var basePath = _configurationService.GetDataPath();
                var usersPath = Path.Combine(basePath, "users.txt");
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

        private async Task LoadIPRecordsAsync()
        {
            try
            {
                var filePath = GetFilePath(SelectedUserId);
                if (System.IO.File.Exists(filePath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    IPRecords = _ipParserService.ParseCSVContent(content);
                    OriginalCSVContent = content;
                    CSVContent = content;
                    FileExists = true;
                }
                else
                {
                    IPRecords = new List<IPRecord>();
                    OriginalCSVContent = string.Empty;
                    CSVContent = string.Empty;
                    FileExists = false;
                }
            }
            catch
            {
                IPRecords = new List<IPRecord>();
                OriginalCSVContent = string.Empty;
                CSVContent = string.Empty;
                FileExists = false;
            }
        }

        private async Task<bool> SetIPsAsync(string csvContent, string? userId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var fileSize = Encoding.UTF8.GetByteCount(csvContent);
                if (fileSize > 10 * 1024 * 1024) // 10MB
                    return false;

                var filePath = GetFilePath(userId);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = filePath + ".tmp";
                await System.IO.File.WriteAllTextAsync(tempPath, csvContent, Encoding.UTF8);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Replace(tempPath, filePath, null);
                }
                else
                {
                    System.IO.File.Move(tempPath, filePath);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<bool> DeleteIPsAsync(string? userId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var filePath = GetFilePath(userId);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string GetFilePath(string? userId)
        {
            var basePath = _configurationService.GetDataPath();
            return string.IsNullOrEmpty(userId)
                ? Path.Combine(basePath, "cloudflare-ip.csv")
                : Path.Combine(basePath, userId, "cloudflare-ip.csv");
        }
    }
}

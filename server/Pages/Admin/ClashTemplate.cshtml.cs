using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace ClashSubManager.Pages.Admin
{
    public class ClashTemplateModel : PageModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ClashTemplateModel> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }

        public List<string> AvailableUsers { get; set; } = new();
        public string YAMLContent { get; set; } = string.Empty;
        public bool FileExists { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "YAML content is required")]
        public string EditedContent { get; set; } = string.Empty;

        public ClashTemplateModel(IConfigurationService configurationService, ILogger<ClashTemplateModel> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            _logger.LogInformation("OnPostSaveAsync called");
            _logger.LogInformation("EditedContent length: {Length}", EditedContent?.Length ?? 0);
            _logger.LogInformation("SelectedUserId: {UserId}", SelectedUserId);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {Error}", error.ErrorMessage);
                }
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            if (!IsValidYAML(EditedContent))
            {
                _logger.LogWarning("YAML validation failed");
                ModelState.AddModelError(nameof(EditedContent), "Invalid YAML format");
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            var result = await SaveYAMLContentAsync(EditedContent, SelectedUserId);
            _logger.LogInformation("SaveYAMLContentAsync result: {Result}", result);

            if (result)
            {
                if (TempData != null)
                {
                    TempData["Success"] = "Template saved successfully";
                    _logger.LogInformation("Success message set in TempData");
                }
            }
            else
            {
                _logger.LogError("Failed to save template");
                ModelState.AddModelError(string.Empty, "Failed to save template");
            }

            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload");
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            if (file.Length > 1024 * 1024) // 1MB
            {
                ModelState.AddModelError(string.Empty, "File size exceeds 1MB limit");
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            if (!IsValidYAML(content))
            {
                ModelState.AddModelError(string.Empty, "Invalid YAML format");
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            var result = await SaveYAMLContentAsync(content, SelectedUserId);

            if (result)
            {
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = "File uploaded successfully";
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to upload file");
            }

            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var result = await DeleteYAMLFileAsync(SelectedUserId);

            if (result)
            {
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = "Template deleted successfully";
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to delete template");
            }

            await LoadUserListAsync();
            await LoadYAMLContentAsync();
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

        private async Task LoadYAMLContentAsync()
        {
            try
            {
                var filePath = GetFilePath(SelectedUserId);
                if (System.IO.File.Exists(filePath))
                {
                    YAMLContent = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    EditedContent = YAMLContent; // Set EditedContent to display in editor
                    FileExists = true;
                }
                else
                {
                    YAMLContent = string.Empty;
                    EditedContent = string.Empty; // Clear EditedContent
                    FileExists = false;
                }
            }
            catch
            {
                YAMLContent = string.Empty;
                EditedContent = string.Empty; // Clear EditedContent
                FileExists = false;
            }
        }

        private async Task<bool> SaveYAMLContentAsync(string content, string userId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var fileSize = Encoding.UTF8.GetByteCount(content);
                if (fileSize > 1024 * 1024) // 1MB
                    return false;

                if (!IsValidYAML(content))
                    return false;

                var filePath = GetFilePath(userId);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = filePath + ".tmp";
                await System.IO.File.WriteAllTextAsync(tempPath, content, Encoding.UTF8);

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

        private async Task<bool> DeleteYAMLFileAsync(string userId)
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

        private string GetFilePath(string userId)
        {
            var basePath = _configurationService.GetDataPath();
            return string.IsNullOrEmpty(userId)
                ? Path.Combine(basePath, "clash.yaml")
                : Path.Combine(basePath, userId, "clash.yaml");
        }

        private bool IsValidYAML(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                using var reader = new StringReader(content);
                var yaml = new YamlStream();
                yaml.Load(reader);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

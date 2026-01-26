using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace ClashSubManager.Pages.Admin
{
    public class ClashTemplateModel : PageModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFileLockProvider _fileLockProvider;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<ClashTemplateModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }

        public List<string> AvailableUsers { get; set; } = new();
        public string YAMLContent { get; set; } = string.Empty;
        public bool FileExists { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "YAMLContentRequired")]
        public string EditedContent { get; set; } = string.Empty;

        public ClashTemplateModel(IConfigurationService configurationService, IFileLockProvider fileLockProvider, IStringLocalizer<SharedResources> localizer, ILogger<ClashTemplateModel> logger)
        {
            _configurationService = configurationService;
            _fileLockProvider = fileLockProvider;
            _localizer = localizer;
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
            _logger.LogDebug("Template save request received. SelectedUserId: {SelectedUserId}, ContentLength: {ContentLength}, IsModelValid: {IsModelValid}",
                SelectedUserId,
                EditedContent?.Length ?? 0,
                ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Template save rejected due to invalid model state. SelectedUserId: {SelectedUserId}", SelectedUserId);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {Error}", error.ErrorMessage);
                }
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            if (!IsValidYAML(EditedContent!))
            {
                _logger.LogWarning("Template save rejected due to invalid YAML format. SelectedUserId: {SelectedUserId}", SelectedUserId);
                ModelState.AddModelError(nameof(EditedContent), _localizer["InvalidYAMLFormat"]);
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            var result = await SaveYAMLContentAsync(EditedContent!, SelectedUserId!);
            _logger.LogDebug("Template save operation result. SelectedUserId: {SelectedUserId}, Result: {Result}", SelectedUserId, result);

            if (result)
            {
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["TemplateSavedSuccessfully"].ToString();
                    _logger.LogDebug("Template save success message set in TempData. SelectedUserId: {SelectedUserId}", SelectedUserId);
                }
            }
            else
            {
                _logger.LogError("Failed to save template. SelectedUserId: {SelectedUserId}", SelectedUserId);
                ModelState.AddModelError(string.Empty, _localizer["FailedToSaveTemplate"]);
            }

            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
        {
            // Clear EditedContent validation error since file upload doesn't submit this field
            ModelState.Remove(nameof(EditedContent));
            
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, _localizer["PleaseSelectFileToUpload"]);
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            if (file.Length > 1024 * 1024) // 1MB
            {
                ModelState.AddModelError(string.Empty, _localizer["FileSizeExceedsLimit"]);
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            if (!IsValidYAML(content))
            {
                ModelState.AddModelError(string.Empty, _localizer["InvalidYAMLFormat"]);
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            var result = await SaveYAMLContentAsync(content, SelectedUserId!);

            if (result)
            {
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
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // Clear EditedContent validation error since delete doesn't need this field
            ModelState.Remove(nameof(EditedContent));
            
            var result = await DeleteYAMLFileAsync(SelectedUserId!);

            if (result)
            {
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = _localizer["TemplateDeletedSuccessfully"].ToString();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer["FailedToDeleteTemplate"]);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user list. SelectedUserId: {SelectedUserId}", SelectedUserId);
                AvailableUsers = new List<string>();
            }
        }

        private async Task LoadYAMLContentAsync()
        {
            try
            {
                var filePath = GetFilePath(SelectedUserId!);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load template content. SelectedUserId: {SelectedUserId}", SelectedUserId);
                YAMLContent = string.Empty;
                EditedContent = string.Empty; // Clear EditedContent
                FileExists = false;
            }
        }

        private async Task<bool> SaveYAMLContentAsync(string content, string userId)
        {
            try
            {
                var fileSize = Encoding.UTF8.GetByteCount(content);
                if (fileSize > 1024 * 1024) // 1MB
                {
                    _logger.LogWarning("Template save rejected: content too large. SizeBytes: {SizeBytes}, SelectedUserId: {SelectedUserId}", fileSize, userId);
                    return false;
                }

                if (!IsValidYAML(content))
                {
                    _logger.LogWarning("Template save rejected: invalid YAML content. SelectedUserId: {SelectedUserId}", userId);
                    return false;
                }

                var filePath = GetFilePath(userId);
                await using var fileLock = await _fileLockProvider.AcquireAsync(filePath);
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

                _logger.LogInformation("Template saved successfully. SelectedUserId: {SelectedUserId}, SizeBytes: {SizeBytes}", userId, fileSize);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save template. SelectedUserId: {SelectedUserId}", userId);
                return false;
            }
        }

        private async Task<bool> DeleteYAMLFileAsync(string userId)
        {
            try
            {
                var filePath = GetFilePath(userId);
                await using var fileLock = await _fileLockProvider.AcquireAsync(filePath);
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "YAML validation failed");
                return false;
            }
        }
    }
}

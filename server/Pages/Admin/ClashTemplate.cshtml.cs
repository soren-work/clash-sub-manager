using ClashSubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace ClashSubManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ClashTemplateModel : PageModel
    {
        private readonly string _basePath = "/app/data";
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [BindProperty(SupportsGet = true)]
        public string SelectedUserId { get; set; }

        public List<string> AvailableUsers { get; set; } = new();
        public string YAMLContent { get; set; }
        public bool FileExists { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "YAML content is required")]
        public string EditedContent { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            if (!IsValidYAML(EditedContent))
            {
                ModelState.AddModelError(nameof(EditedContent), "Invalid YAML format");
                await LoadUserListAsync();
                await LoadYAMLContentAsync();
                return Page();
            }

            var result = await SaveYAMLContentAsync(EditedContent, SelectedUserId);
            
            if (result)
            {
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = "Template saved successfully";
                }
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to save template");
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
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to upload file");
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
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to delete template");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
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

        private async Task LoadYAMLContentAsync()
        {
            try
            {
                var filePath = GetFilePath(SelectedUserId);
                if (System.IO.File.Exists(filePath))
                {
                    YAMLContent = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    FileExists = true;
                }
                else
                {
                    YAMLContent = string.Empty;
                    FileExists = false;
                }
            }
            catch
            {
                YAMLContent = string.Empty;
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
                if (!Directory.Exists(directory))
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
            return string.IsNullOrEmpty(userId) 
                ? Path.Combine(_basePath, "clash.yaml")
                : Path.Combine(_basePath, userId, "clash.yaml");
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

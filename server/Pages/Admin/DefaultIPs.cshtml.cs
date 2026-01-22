using ClashSubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClashSubManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DefaultIPsModel : PageModel
    {
        private readonly string _basePath = "/app/data";
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [BindProperty(SupportsGet = true)]
        public string SelectedUserId { get; set; } = string.Empty;

        public List<IPRecord> IPRecords { get; set; } = new();
        public List<string> AvailableUsers { get; set; } = new();
        public bool FileExists { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "CSV content is required")]
        public string CSVContent { get; set; } = string.Empty;

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
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = "IP configuration saved successfully";
                }
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to save IP configuration");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload");
                await LoadUserListAsync();
                await LoadIPRecordsAsync();
                return Page();
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                ModelState.AddModelError(string.Empty, "File size exceeds 10MB limit");
                await LoadUserListAsync();
                await LoadIPRecordsAsync();
                return Page();
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            
            var result = await SetIPsAsync(content, SelectedUserId);
            
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
            await LoadIPRecordsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteIPsAsync()
        {
            var result = await DeleteIPsAsync(SelectedUserId);
            
            if (result)
            {
                // Check if TempData is available
                if (TempData != null)
                {
                    TempData["Success"] = "Configuration deleted successfully";
                }
                return RedirectToPage();
            }
            
            ModelState.AddModelError(string.Empty, "Failed to delete configuration");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
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

        private async Task LoadIPRecordsAsync()
        {
            try
            {
                var filePath = GetFilePath(SelectedUserId);
                if (System.IO.File.Exists(filePath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    IPRecords = ParseCSVContent(content);
                    FileExists = true;
                }
                else
                {
                    IPRecords = new List<IPRecord>();
                    FileExists = false;
                }
            }
            catch
            {
                IPRecords = new List<IPRecord>();
                FileExists = false;
            }
        }

        private async Task<bool> SetIPsAsync(string csvContent, string userId)
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

        private async Task<bool> DeleteIPsAsync(string userId)
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
                ? Path.Combine(_basePath, "cloudflare-ip.csv")
                : Path.Combine(_basePath, userId, "cloudflare-ip.csv");
        }

        private List<IPRecord> ParseCSVContent(string csvContent)
        {
            var records = new List<IPRecord>();
            
            if (string.IsNullOrWhiteSpace(csvContent))
                return records;

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var isFirstLine = true;
            var id = 1;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                if (isFirstLine && trimmedLine.Contains("IP地址"))
                {
                    isFirstLine = false;
                    continue;
                }
                
                isFirstLine = false;
                var record = ParseLineToIPRecord(trimmedLine);
                if (record != null)
                {
                    record.Id = id++;
                    records.Add(record);
                }
            }

            return records;
        }

        private IPRecord? ParseLineToIPRecord(string line)
        {
            var columns = line.Split(',');
            if (columns.Length < 1 || !IsValidIP(columns[0].Trim()))
                return null;

            var record = new IPRecord
            {
                IPAddress = columns[0].Trim()
            };

            if (columns.Length > 1) record.Sent = columns[1].Trim();
            if (columns.Length > 2) record.Received = columns[2].Trim();
            if (columns.Length > 3) record.PacketLossRate = columns[3].Trim();
            if (columns.Length > 4) record.AverageLatency = columns[4].Trim();
            if (columns.Length > 5) record.DownloadSpeed = columns[5].Trim();

            return record;
        }

        private bool IsValidIP(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }
    }
}

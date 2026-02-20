# Default Optimized IP Management MVP Detailed Design

> **📌 Document Status**: MVP completed, this document is retained as technical reference  
> **🎯 Target Audience**: Developers, contributors  
> **📅 Last Updated**: 2026-02-20  
> **💡 Tip**: For feature usage, please refer to [Advanced Guide](../../../advanced-guide.md)

**🌐 Language**: [English](ip-management-detail.md) | [中文](ip-management-detail-cn.md)

## 1. MVP Core Functions

### 1.1 Necessary Function List
- **IP management page**: `/Admin/DefaultIPs` - Unified management interface
- **User selector**: Dropdown to select global or specific user for operations
- **CSV content management**: Direct CSV content editing in text area, support paste and upload
- **IP list display**: Display optimized IPs and speed test data in table format
- **Smart data rendering**: Support CloudflareST format, missing data shows "No Data"
- **IP configuration deletion**: Delete entire CSV file
- **Real-time effect**: Changes immediately affect user subscription generation
- **File path management**: Global (/app/data/cloudflare-ip.csv) and user-specific paths

### 1.2 Implementation Priority
1. **High priority**: IP management page and user selector
2. **High priority**: CSV content management and IP list display
3. **Medium priority**: File upload functionality
4. **Medium priority**: Smart data parsing

### 1.3 Technical Constraints
- **ASP.NET Core Razor Pages**: Only use PageModel
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **Form submission**: Use standard form POST submission
- **File operations**: Direct file API, avoid over-abstraction
- **Function length**: ≤50 lines
- **Nesting limit**: ≤3 layers

## 2. Razor Pages Implementation Design

### 2.1 DefaultIPs Page (/Admin/DefaultIPs.cshtml)
```csharp
[Authorize(Roles = "Admin")]
public class DefaultIPsModel : PageModel
{
    private readonly string _basePath = "/app/data";
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    [BindProperty(SupportsGet = true)]
    public string SelectedUserId { get; set; }

    public List<IPRecord> IPRecords { get; set; } = new();
    public List<string> AvailableUsers { get; set; } = new();
    public bool FileExists { get; set; }

    [BindProperty]
    public string CSVContent { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserListAsync();
        await LoadIPRecordsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSetIPsAsync()
    {
        if (string.IsNullOrWhiteSpace(CSVContent))
        {
            ModelState.AddModelError(nameof(CSVContent), "CSV content cannot be empty");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        var result = await SetIPsAsync(CSVContent, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "IP settings saved successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "IP settings failed");
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
            TempData["Success"] = "File uploaded successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "File upload failed");
        await LoadUserListAsync();
        await LoadIPRecordsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteIPsAsync()
    {
        var result = await DeleteIPsAsync(SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "Configuration deleted successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "Configuration deletion failed");
        await LoadUserListAsync();
        await LoadIPRecordsAsync();
        return Page();
    }

    private async Task LoadUserListAsync()
    {
        try
        {
            var usersPath = Path.Combine(_basePath, "users.txt");
            if (File.Exists(usersPath))
            {
                var content = await File.ReadAllTextAsync(usersPath, Encoding.UTF8);
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
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
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
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, csvContent, Encoding.UTF8);
            
            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
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
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
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

            if (isFirstLine && trimmedLine.Contains("IP Address"))
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

    private IPRecord ParseLineToIPRecord(string line)
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
```

### 2.2 Data Model
```csharp
public class IPRecord
{
    public int Id { get; set; }
    public string IPAddress { get; set; }
    public string Sent { get; set; } = "No Data";
    public string Received { get; set; } = "No Data";
    public string PacketLossRate { get; set; } = "No Data";
    public string AverageLatency { get; set; } = "No Data";
    public string DownloadSpeed { get; set; } = "No Data";
}
```

## 3. Frontend Interface Design

### 3.1 DefaultIPs.cshtml View
```cshtml
@page "/admin/default-ips"
@model DefaultIPsModel
@{
    ViewData["Title"] = "Optimized IP Management";
}

<div class="container-fluid">
    <h2>Optimized IP Management</h2>
    
    @if (!string.IsNullOrEmpty(TempData["Success"] as string))
    {
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            @TempData["Success"]
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }

    @if (!ModelState.IsValid)
    {
        <div class="alert alert-danger">
            @foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                <div>@error.ErrorMessage</div>
            }
        </div>
    }

    <!-- User selector -->
    <div class="row mb-3">
        <div class="col-md-6">
            <label class="form-label">Select User</label>
            <select asp-for="SelectedUserId" class="form-select" onchange="location.href='?SelectedUserId=' + this.value">
                <option value="">Global Configuration</option>
                @foreach (var user in Model.AvailableUsers)
                {
                    <option value="@user">@user</option>
                }
            </select>
        </div>
    </div>

    <!-- File status -->
    <div class="row mb-3">
        <div class="col-12">
            <div class="alert alert-info">
                Current status: @(Model.FileExists ? "File exists" : "File does not exist")
                @if (Model.FileExists)
                {
                    <span> | IP count: @Model.IPRecords.Count</span>
                }
            </div>
        </div>
    </div>

    <!-- CSV content management -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">CSV Content (Supports CloudflareST format)</label>
                    <textarea asp-for="CSVContent" class="form-control font-monospace" rows="10" 
                              placeholder="Please paste CSV content or upload result.csv file..."></textarea>
                    <div class="form-text">Supports result.csv format output by CloudflareST program, file size limit 10MB</div>
                </div>
                <div class="d-flex gap-2 mb-3">
                    <button type="submit" asp-page-handler="SetIPs" class="btn btn-primary">Save Configuration</button>
                    <button type="submit" asp-page-handler="DeleteIPs" class="btn btn-danger" 
                            onclick="return confirm('Are you sure you want to delete this configuration?')">Delete Configuration</button>
                </div>
            </form>
        </div>
    </div>

    <!-- File upload -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post" enctype="multipart/form-data">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">Or upload CSV file</label>
                    <input type="file" name="file" class="form-control" accept=".csv" />
                    <div class="form-text">Supports .csv files, maximum 10MB</div>
                </div>
                <button type="submit" asp-page-handler="Upload" class="btn btn-secondary">Upload File</button>
            </form>
        </div>
    </div>

    <!-- IP list display -->
    <div class="row">
        <div class="col-12">
            <h5>Current IP List (@Model.IPRecords.Count items)</h5>
            @if (Model.IPRecords.Any())
            {
                <div class="table-responsive">
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>IP Address</th>
                                <th>Sent</th>
                                <th>Received</th>
                                <th>Packet Loss Rate</th>
                                <th>Average Latency</th>
                                <th>Download Speed</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var ip in Model.IPRecords)
                            {
                                <tr>
                                    <td>@ip.Id</td>
                                    <td>@ip.IPAddress</td>
                                    <td>@ip.Sent</td>
                                    <td>@ip.Received</td>
                                    <td>@ip.PacketLossRate</td>
                                    <td>@ip.AverageLatency</td>
                                    <td>@ip.DownloadSpeed</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
            else
            {
                <div class="text-muted">No IP configuration available</div>
            }
        </div>
    </div>
</div>
```

## 4. MVP Constraint Check

### 4.1 Architecture Compliance
- ✅ **Razor Pages mode**: Use PageModel instead of API controllers
- ✅ **Monolithic application**: No frontend-backend separation, server-side rendering
- ✅ **Simplified design**: Direct file operations, no over-abstraction
- ✅ **Form submission**: Use standard form POST submission

### 4.2 Technical Constraint Compliance
- ✅ **.NET 10 + Razor Pages**: Conforms to technology selection
- ✅ **Bootstrap frontend**: Use Bootstrap styles
- ✅ **Function length ≤50 lines**: All methods meet requirements
- ✅ **Nesting ≤3 layers**: Code structure is concise

### 4.3 MVP Optimization Results
- ✅ **Remove complex service layer**: Implement logic directly in PageModel
- ✅ **Simplify file management**: Use native File API
- ✅ **Unified interface design**: Single page manages all IP operations
- ✅ **Conform to monolithic architecture**: Strictly prohibit frontend-backend separation

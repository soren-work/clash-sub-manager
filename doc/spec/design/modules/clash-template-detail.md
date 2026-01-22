# Clash Template File Management MVP Detailed Design

**🌐 Language**: [English](clash-template-detail.md) | [中文](clash-template-detail-cn.md)

## 1. MVP Core Functions

### 1.1 Necessary Function List
- **Template management page**: `/Admin/ClashTemplate` - Unified management interface
- **Global template management**: CRUD operations for `/app/data/clash.yaml` file
- **User-specific template management**: CRUD operations for `/app/data/[user id]/clash.yaml` file
- **User selector**: Dropdown to select global or specific user for operations
- **Content editing**: Direct YAML content editing in text area
- **File upload**: Support file upload to overwrite content
- **Template deletion**: Delete entire YAML file
- **Basic validation**: YAML format validation and file size limit (1MB)

### 1.2 Implementation Priority
1. **High priority**: Template management page and user selector
2. **High priority**: CRUD operations for global and user-specific templates
3. **Medium priority**: File upload functionality
4. **Medium priority**: Basic YAML format validation

### 1.3 Technical Constraints
- **ASP.NET Core Razor Pages**: Only use PageModel
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **Form submission**: Use standard form POST submission
- **File operations**: Direct file API, avoid over-abstraction
- **Function length**: ≤50 lines
- **Nesting limit**: ≤3 layers

## 2. Razor Pages Implementation Design

### 2.1 ClashTemplate Page (/Admin/ClashTemplate.cshtml)
```csharp
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
    public string EditedContent { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserListAsync();
        await LoadYAMLContentAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditedContent))
        {
            ModelState.AddModelError(nameof(EditedContent), "YAML content cannot be empty");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        var result = await SaveYAMLContentAsync(EditedContent, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "Template saved successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "Template save failed");
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
            TempData["Success"] = "File uploaded successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "File upload failed");
        await LoadUserListAsync();
        await LoadYAMLContentAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var result = await DeleteYAMLFileAsync(SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "Template deleted successfully";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "Template deletion failed");
        await LoadUserListAsync();
        await LoadYAMLContentAsync();
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

    private async Task LoadYAMLContentAsync()
    {
        try
        {
            var filePath = GetFilePath(SelectedUserId);
            if (File.Exists(filePath))
            {
                YAMLContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
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
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8);
            
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

    private async Task<bool> DeleteYAMLFileAsync(string userId)
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
            ? Path.Combine(_basePath, "clash.yaml")
            : Path.Combine(_basePath, userId, "clash.yaml");
    }

    private bool IsValidYAML(string content)
    {
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
```

## 3. Frontend Interface Design

### 3.1 ClashTemplate.cshtml View
```cshtml
@page "/admin/clash-template"
@model ClashTemplateModel
@{
    ViewData["Title"] = "Clash Template Management";
}

<div class="container-fluid">
    <h2>Clash Template Management</h2>
    
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
                <option value="">Global Template</option>
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
                    <span> | File path: @Model.GetFilePath(Model.SelectedUserId)</span>
                }
            </div>
        </div>
    </div>

    <!-- YAML content editing -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">YAML Template Content</label>
                    <textarea asp-for="EditedContent" class="form-control font-monospace" rows="20" 
                              placeholder="Please enter YAML content...">@Model.YAMLContent</textarea>
                    <div class="form-text">Supports all Clash configuration fields, file size limit 1MB</div>
                </div>
                <div class="d-flex gap-2 mb-3">
                    <button type="submit" asp-page-handler="Save" class="btn btn-primary">Save Template</button>
                    <button type="submit" asp-page-handler="Delete" class="btn btn-danger" 
                            onclick="return confirm('Are you sure you want to delete this template?')">Delete Template</button>
                </div>
            </form>
        </div>
    </div>

    <!-- File upload -->
    <div class="row">
        <div class="col-12">
            <form method="post" enctype="multipart/form-data">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">Or upload YAML file</label>
                    <input type="file" name="file" class="form-control" accept=".yaml,.yml" />
                    <div class="form-text">Supports .yaml and .yml files, maximum 1MB</div>
                </div>
                <button type="submit" asp-page-handler="Upload" class="btn btn-secondary">Upload File</button>
            </form>
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
- ✅ **Remove complex service layer**: Implement file operations directly in PageModel
- ✅ **Simplify file management**: Use native File API
- ✅ **Unified interface design**: Single page manages all template operations
- ✅ **Conform to monolithic architecture**: Strictly prohibit frontend-backend separation

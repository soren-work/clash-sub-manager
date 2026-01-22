# Clash模板文件管理 MVP详细设计

**🌐 语言**: [English](clash-template-detail.md) | [中文](clash-template-detail-cn.md)

## 1. MVP核心功能

### 1.1 必要功能清单
- **模板管理页面**：`/Admin/ClashTemplate` - 统一管理界面
- **全局模板管理**：`/app/data/clash.yaml`文件的增删改查
- **用户专属模板管理**：`/app/data/[用户id]/clash.yaml`文件的增删改查
- **用户选择器**：下拉选择全局或特定用户进行操作
- **内容编辑**：文本域直接编辑YAML内容
- **文件上传**：支持文件上传覆盖内容
- **模板删除**：删除整个YAML文件
- **基础验证**：YAML格式验证和文件大小限制（1MB）

### 1.2 实现优先级
1. **高优先级**：模板管理页面和用户选择器
2. **高优先级**：全局和用户专属模板的增删改查
3. **中优先级**：文件上传功能
4. **中优先级**：基础YAML格式验证

### 1.3 技术约束
- **ASP.NET Core Razor Pages**：仅使用PageModel
- **单体应用架构**：严禁前后端分离
- **表单提交**：使用标准表单POST提交
- **文件操作**：直接文件API，避免过度抽象
- **函数长度**：≤50行
- **嵌套限制**：≤3层

## 2. Razor Pages实现设计

### 2.1 ClashTemplate页面 (/Admin/ClashTemplate.cshtml)
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
            ModelState.AddModelError(nameof(EditedContent), "YAML内容不能为空");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        var result = await SaveYAMLContentAsync(EditedContent, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "模板保存成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "模板保存失败");
        await LoadUserListAsync();
        await LoadYAMLContentAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "请选择要上传的文件");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        if (file.Length > 1024 * 1024) // 1MB
        {
            ModelState.AddModelError(string.Empty, "文件大小超过1MB限制");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        
        if (!IsValidYAML(content))
        {
            ModelState.AddModelError(string.Empty, "YAML格式无效");
            await LoadUserListAsync();
            await LoadYAMLContentAsync();
            return Page();
        }

        var result = await SaveYAMLContentAsync(content, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "文件上传成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "文件上传失败");
        await LoadUserListAsync();
        await LoadYAMLContentAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var result = await DeleteYAMLFileAsync(SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "模板删除成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "模板删除失败");
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

## 3. 前端界面设计

### 3.1 ClashTemplate.cshtml 视图
```cshtml
@page "/admin/clash-template"
@model ClashTemplateModel
@{
    ViewData["Title"] = "Clash模板管理";
}

<div class="container-fluid">
    <h2>Clash模板管理</h2>
    
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

    <!-- 用户选择器 -->
    <div class="row mb-3">
        <div class="col-md-6">
            <label class="form-label">选择用户</label>
            <select asp-for="SelectedUserId" class="form-select" onchange="location.href='?SelectedUserId=' + this.value">
                <option value="">全局模板</option>
                @foreach (var user in Model.AvailableUsers)
                {
                    <option value="@user">@user</option>
                }
            </select>
        </div>
    </div>

    <!-- 文件状态 -->
    <div class="row mb-3">
        <div class="col-12">
            <div class="alert alert-info">
                当前状态：@(Model.FileExists ? "文件存在" : "文件不存在")
                @if (Model.FileExists)
                {
                    <span> | 文件路径：@Model.GetFilePath(Model.SelectedUserId)</span>
                }
            </div>
        </div>
    </div>

    <!-- YAML内容编辑 -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">YAML模板内容</label>
                    <textarea asp-for="EditedContent" class="form-control font-monospace" rows="20" 
                              placeholder="请输入YAML内容...">@Model.YAMLContent</textarea>
                    <div class="form-text">支持所有Clash配置字段，文件大小限制1MB</div>
                </div>
                <div class="d-flex gap-2 mb-3">
                    <button type="submit" asp-page-handler="Save" class="btn btn-primary">保存模板</button>
                    <button type="submit" asp-page-handler="Delete" class="btn btn-danger" 
                            onclick="return confirm('确定要删除这个模板吗？')">删除模板</button>
                </div>
            </form>
        </div>
    </div>

    <!-- 文件上传 -->
    <div class="row">
        <div class="col-12">
            <form method="post" enctype="multipart/form-data">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">或上传YAML文件</label>
                    <input type="file" name="file" class="form-control" accept=".yaml,.yml" />
                    <div class="form-text">支持.yaml和.yml文件，最大1MB</div>
                </div>
                <button type="submit" asp-page-handler="Upload" class="btn btn-secondary">上传文件</button>
            </form>
        </div>
    </div>
</div>
```

## 4. MVP约束检查

### 4.1 架构合规性
- ✅ **Razor Pages模式**：使用PageModel而非API控制器
- ✅ **单体应用**：无前后端分离，服务端渲染
- ✅ **简化设计**：直接文件操作，无过度抽象
- ✅ **表单提交**：使用标准表单POST提交

### 4.2 技术约束合规性
- ✅ **.NET 10 + Razor Pages**：符合技术选型
- ✅ **Bootstrap前端**：使用Bootstrap样式
- ✅ **函数长度≤50行**：所有方法符合要求
- ✅ **嵌套≤3层**：代码结构简洁

### 4.3 MVP优化成果
- ✅ **移除复杂服务层**：直接在PageModel中实现文件操作
- ✅ **简化文件管理**：使用原生File API
- ✅ **统一界面设计**：单一页面管理所有模板操作
- ✅ **符合单体架构**：严禁前后端分离

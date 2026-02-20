# 默认优选IP管理MVP详细设计

> **📌 文档状态**: MVP已完成，本文档作为技术参考保留  
> **🎯 目标读者**: 开发者、贡献者  
> **📅 最后更新**: 2026-02-20  
> **💡 提示**: 如需了解功能使用，请参阅[高级指南](../../../advanced-guide-cn.md)

**🌐 语言**: [English](ip-management-detail.md) | [中文](ip-management-detail-cn.md)

## 1. MVP核心功能

### 1.1 必要功能清单
- **IP管理页面**：`/Admin/DefaultIPs` - 统一管理界面
- **用户选择器**：下拉选择全局或特定用户进行操作
- **CSV内容管理**：文本域直接编辑CSV内容，支持粘贴和上传
- **IP列表展示**：表格形式显示优选IP及测速数据
- **智能数据渲染**：支持CloudflareST格式，缺失数据显示"无数据"
- **IP配置删除**：删除整个CSV文件
- **实时生效**：修改后立即影响用户订阅生成
- **文件路径管理**：全局(/app/data/cloudflare-ip.csv)和用户特定路径

### 1.2 实现优先级
1. **高优先级**：IP管理页面和用户选择器
2. **高优先级**：CSV内容管理和IP列表展示
3. **中优先级**：文件上传功能
4. **中优先级**：智能数据解析

### 1.3 技术约束
- **ASP.NET Core Razor Pages**：仅使用PageModel
- **单体应用架构**：严禁前后端分离
- **表单提交**：使用标准表单POST提交
- **文件操作**：直接文件API，避免过度抽象
- **函数长度**：≤50行
- **嵌套限制**：≤3层

## 2. Razor Pages实现设计

### 2.1 DefaultIPs页面 (/Admin/DefaultIPs.cshtml)
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
            ModelState.AddModelError(nameof(CSVContent), "CSV内容不能为空");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        var result = await SetIPsAsync(CSVContent, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "IP设置成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "IP设置失败");
        await LoadUserListAsync();
        await LoadIPRecordsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "请选择要上传的文件");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB
        {
            ModelState.AddModelError(string.Empty, "文件大小超过10MB限制");
            await LoadUserListAsync();
            await LoadIPRecordsAsync();
            return Page();
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        
        var result = await SetIPsAsync(content, SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "文件上传成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "文件上传失败");
        await LoadUserListAsync();
        await LoadIPRecordsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteIPsAsync()
    {
        var result = await DeleteIPsAsync(SelectedUserId);
        
        if (result)
        {
            TempData["Success"] = "配置删除成功";
            return RedirectToPage();
        }
        
        ModelState.AddModelError(string.Empty, "配置删除失败");
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

### 2.2 数据模型
```csharp
public class IPRecord
{
    public int Id { get; set; }
    public string IPAddress { get; set; }
    public string Sent { get; set; } = "无数据";
    public string Received { get; set; } = "无数据";
    public string PacketLossRate { get; set; } = "无数据";
    public string AverageLatency { get; set; } = "无数据";
    public string DownloadSpeed { get; set; } = "无数据";
}
```

## 3. 前端界面设计

### 3.1 DefaultIPs.cshtml 视图
```cshtml
@page "/admin/default-ips"
@model DefaultIPsModel
@{
    ViewData["Title"] = "优选IP管理";
}

<div class="container-fluid">
    <h2>优选IP管理</h2>
    
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
                <option value="">全局配置</option>
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
                    <span> | IP数量：@Model.IPRecords.Count</span>
                }
            </div>
        </div>
    </div>

    <!-- CSV内容管理 -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">CSV内容（支持CloudflareST格式）</label>
                    <textarea asp-for="CSVContent" class="form-control font-monospace" rows="10" 
                              placeholder="请粘贴CSV内容或上传result.csv文件..."></textarea>
                    <div class="form-text">支持CloudflareST程序输出的result.csv格式，文件大小限制10MB</div>
                </div>
                <div class="d-flex gap-2 mb-3">
                    <button type="submit" asp-page-handler="SetIPs" class="btn btn-primary">保存配置</button>
                    <button type="submit" asp-page-handler="DeleteIPs" class="btn btn-danger" 
                            onclick="return confirm('确定要删除这个配置吗？')">删除配置</button>
                </div>
            </form>
        </div>
    </div>

    <!-- 文件上传 -->
    <div class="row mb-3">
        <div class="col-12">
            <form method="post" enctype="multipart/form-data">
                <input type="hidden" asp-for="SelectedUserId" />
                <div class="mb-3">
                    <label class="form-label">或上传CSV文件</label>
                    <input type="file" name="file" class="form-control" accept=".csv" />
                    <div class="form-text">支持.csv文件，最大10MB</div>
                </div>
                <button type="submit" asp-page-handler="Upload" class="btn btn-secondary">上传文件</button>
            </form>
        </div>
    </div>

    <!-- IP列表展示 -->
    <div class="row">
        <div class="col-12">
            <h5>当前IP列表 (@Model.IPRecords.Count 个)</h5>
            @if (Model.IPRecords.Any())
            {
                <div class="table-responsive">
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>IP地址</th>
                                <th>已发送</th>
                                <th>已接收</th>
                                <th>丢包率</th>
                                <th>平均延迟</th>
                                <th>下载速度</th>
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
                <div class="text-muted">暂无IP配置</div>
            }
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
- ✅ **移除复杂服务层**：直接在PageModel中实现逻辑
- ✅ **简化文件管理**：使用原生File API
- ✅ **统一界面设计**：单一页面管理所有IP操作
- ✅ **符合单体架构**：严禁前后端分离

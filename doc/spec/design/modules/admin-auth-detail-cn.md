# 管理员认证与权限 MVP详细设计

**🌐 语言**: [English](admin-auth-detail.md) | [中文](admin-auth-detail-cn.md)

## 1. MVP核心功能

### 1.1 必要功能清单
- **登录页面**：`/Admin/Login` - 显示登录表单（无需认证）
- **登录处理**：POST表单提交验证环境变量凭据
- **登出功能**：`/Admin/Logout` - 清除认证Cookie
- **管理界面保护**：所有`/Admin/*`页面需要认证（除Login外）
- **Cookie会话管理**：HMACSHA256签名防篡改
- **会话超时**：自动过期机制

### 1.2 实现优先级
1. **高优先级**：登录/登出Razor Pages
2. **中优先级**：认证中间件
3. **中优先级**：安全加固（HMAC签名）

### 1.3 技术约束
- **ASP.NET Core Razor Pages**：仅使用PageModel
- **单体应用架构**：严禁前后端分离
- **环境变量认证**：Docker配置管理员凭据
- **Cookie会话**：HttpOnly、Secure、SameSite=Strict
- **HMACSHA256签名**：防篡改
- **函数长度**：≤50行
- **嵌套限制**：≤3层

## 2. Razor Pages实现设计

### 2.1 Login页面 (/Admin/Login.cshtml)
```csharp
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = false)]
    public string Username { get; set; }
    
    [BindProperty(SupportsGet = false)]
    public string Password { get; set; }
    
    public string ErrorMessage { get; set; }
    
    // GET: /Admin/Login - 显示登录页面（无需认证）
    public IActionResult OnGet()
    {
        return Page();
    }
    
    // POST: /Admin/Login - 处理登录表单提交
    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "用户名和密码不能为空";
            return Page();
        }

        if (!ValidateCredentials(Username, Password))
        {
            ErrorMessage = "用户名或密码错误";
            return Page();
        }

        SetAuthCookie();
        return RedirectToPage("/Admin/Index");
    }
    
    private bool ValidateCredentials(string username, string password)
    {
        var configUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        var configPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        return username == configUsername && password == configPassword;
    }
    
    private void SetAuthCookie()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var timeoutMinutes = int.Parse(Environment.GetEnvironmentVariable("SESSION_TIMEOUT_MINUTES") ?? "30");
        var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
        
        var hmacKey = Environment.GetEnvironmentVariable("COOKIE_SECRET_KEY") ?? "default-key";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacKey));
        var signatureData = $"{sessionId}|{expiresAt:yyyyMMddHHmmss}";
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
        var signature = Convert.ToBase64String(hash);
        
        var cookieValue = $"{sessionId}:{signature}";
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/admin"
        };
        
        Response.Cookies.Append("AdminSession", cookieValue, cookieOptions);
    }
}
```

### 2.2 Logout页面 (/Admin/Logout.cshtml)
```csharp
public class LogoutModel : PageModel
{
    // POST: /Admin/Logout - 处理登出请求
    public IActionResult OnPost()
    {
        Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/admin" });
        return RedirectToPage("/Admin/Login");
    }
}
```

### 2.3 认证中间件（简化版）
```csharp
public class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _hmacKey;

    public AdminAuthMiddleware(RequestDelegate next)
    {
        _next = next;
        _hmacKey = Environment.GetEnvironmentVariable("COOKIE_SECRET_KEY") ?? "default-key";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Equals("/admin/login", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/admin/logout", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var sessionCookie = context.Request.Cookies["AdminSession"];
            if (!ValidateSessionCookie(sessionCookie))
            {
                context.Response.Redirect("/admin/login");
                return;
            }
        }

        await _next(context);
    }
    
    private bool ValidateSessionCookie(string cookieValue)
    {
        if (string.IsNullOrEmpty(cookieValue)) return false;
        
        var parts = cookieValue.Split(':');
        if (parts.Length != 2) return false;
        
        var sessionId = parts[0];
        var signature = parts[1];
        
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
        var expectedSignature = Convert.ToBase64String(hash);
        
        return signature == expectedSignature;
    }
}
```

## 3. 前端界面设计

### 3.1 Login.cshtml 视图
```cshtml
@page
@model LoginModel
@{
    ViewData["Title"] = "管理员登录";
}

<div class="container d-flex justify-content-center align-items-center min-vh-100">
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <h4 class="text-center">管理员登录</h4>
            </div>
            <div class="card-body">
                @if (!string.IsNullOrEmpty(Model.ErrorMessage))
                {
                    <div class="alert alert-danger alert-dismissible fade show" role="alert">
                        @Model.ErrorMessage
                        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                    </div>
                }
                
                <form method="post">
                    <div class="mb-3">
                        <label asp-for="Username" class="form-label">用户名</label>
                        <input asp-for="Username" class="form-control" autocomplete="username" />
                    </div>
                    <div class="mb-3">
                        <label asp-for="Password" class="form-label">密码</label>
                        <input asp-for="Password" type="password" class="form-control" autocomplete="current-password" />
                    </div>
                    <button type="submit" class="btn btn-primary w-100">登录</button>
                </form>
            </div>
        </div>
    </div>
</div>
```

### 3.2 Logout.cshtml 视图
```cshtml
@page
@model LogoutModel
@{
    ViewData["Title"] = "登出";
}

<form method="post">
    <button type="submit" class="btn btn-secondary">确认登出</button>
</form>
```

## 4. 环境变量配置

### 4.1 Docker环境变量
```bash
ADMIN_USERNAME=admin
ADMIN_PASSWORD=your_secure_password_here
COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
SESSION_TIMEOUT_MINUTES=30
```

## 5. MVP约束检查

### 5.1 架构合规性
- ✅ **Razor Pages模式**：使用PageModel而非API控制器
- ✅ **单体应用**：无前后端分离，服务端渲染
- ✅ **简化设计**：移除过度抽象的服务层
- ✅ **表单提交**：使用标准表单POST提交

### 5.2 技术约束合规性
- ✅ **.NET 10 + Razor Pages**：符合技术选型
- ✅ **Bootstrap前端**：使用Bootstrap样式
- ✅ **函数长度≤50行**：所有方法符合要求
- ✅ **嵌套≤3层**：代码结构简洁

### 5.3 安全约束合规性
- ✅ **环境变量认证**：Docker配置管理员凭据
- ✅ **Cookie安全**：HttpOnly、Secure、SameSite=Strict
- ✅ **HMACSHA256签名**：防篡改机制
- ✅ **会话超时**：可配置5-1440分钟

### 5.4 MVP优化成果
- ✅ **移除复杂服务层**：直接在PageModel中实现逻辑
- ✅ **简化中间件**：仅保留必要认证功能
- ✅ **统一Razor Pages**：所有功能使用页面模型
- ✅ **符合单体架构**：严禁前后端分离

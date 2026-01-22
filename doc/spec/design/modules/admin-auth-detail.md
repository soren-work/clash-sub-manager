# Admin Authentication and Permission MVP Detailed Design

**🌐 Language**: [English](admin-auth-detail.md) | [中文](admin-auth-detail-cn.md)

## 1. MVP Core Functions

### 1.1 Necessary Function List
- **Login page**: `/Admin/Login` - Display login form (no authentication required)
- **Login processing**: POST form submission to validate environment variable credentials
- **Logout function**: `/Admin/Logout` - Clear authentication Cookie
- **Management interface protection**: All `/Admin/*` pages require authentication (except Login)
- **Cookie session management**: HMACSHA256 signature to prevent tampering
- **Session timeout**: Automatic expiration mechanism

### 1.2 Implementation Priority
1. **High priority**: Login/Logout Razor Pages
2. **Medium priority**: Authentication middleware
3. **Medium priority**: Security hardening (HMAC signature)

### 1.3 Technical Constraints
- **ASP.NET Core Razor Pages**: Only use PageModel
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **Environment variable authentication**: Docker configuration for admin credentials
- **Cookie session**: HttpOnly, Secure, SameSite=Strict
- **HMACSHA256 signature**: Anti-tampering
- **Function length**: ≤50 lines
- **Nesting limit**: ≤3 layers

## 2. Razor Pages Implementation Design

### 2.1 Login Page (/Admin/Login.cshtml)
```csharp
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = false)]
    public string Username { get; set; }
    
    [BindProperty(SupportsGet = false)]
    public string Password { get; set; }
    
    public string ErrorMessage { get; set; }
    
    // GET: /Admin/Login - Display login page (no authentication required)
    public IActionResult OnGet()
    {
        return Page();
    }
    
    // POST: /Admin/Login - Handle login form submission
    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Username and password cannot be empty";
            return Page();
        }

        if (!ValidateCredentials(Username, Password))
        {
            ErrorMessage = "Invalid username or password";
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

### 2.2 Logout Page (/Admin/Logout.cshtml)
```csharp
public class LogoutModel : PageModel
{
    // POST: /Admin/Logout - Handle logout request
    public IActionResult OnPost()
    {
        Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/admin" });
        return RedirectToPage("/Admin/Login");
    }
}
```

### 2.3 Authentication Middleware (Simplified)
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

## 3. Frontend Interface Design

### 3.1 Login.cshtml View
```cshtml
@page
@model LoginModel
@{
    ViewData["Title"] = "Admin Login";
}

<div class="container d-flex justify-content-center align-items-center min-vh-100">
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <h4 class="text-center">Admin Login</h4>
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
                        <label asp-for="Username" class="form-label">Username</label>
                        <input asp-for="Username" class="form-control" autocomplete="username" />
                    </div>
                    <div class="mb-3">
                        <label asp-for="Password" class="form-label">Password</label>
                        <input asp-for="Password" type="password" class="form-control" autocomplete="current-password" />
                    </div>
                    <button type="submit" class="btn btn-primary w-100">Login</button>
                </form>
            </div>
        </div>
    </div>
</div>
```

### 3.2 Logout.cshtml View
```cshtml
@page
@model LogoutModel
@{
    ViewData["Title"] = "Logout";
}

<form method="post">
    <button type="submit" class="btn btn-secondary">Confirm Logout</button>
</form>
```

## 4. Environment Variable Configuration

### 4.1 Docker Environment Variables
```bash
ADMIN_USERNAME=admin
ADMIN_PASSWORD=your_secure_password_here
COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
SESSION_TIMEOUT_MINUTES=30
```

## 5. MVP Constraint Check

### 5.1 Architecture Compliance
- ✅ **Razor Pages mode**: Use PageModel instead of API controllers
- ✅ **Monolithic application**: No frontend-backend separation, server-side rendering
- ✅ **Simplified design**: Remove overly abstracted service layer
- ✅ **Form submission**: Use standard form POST submission

### 5.2 Technical Constraint Compliance
- ✅ **.NET 10 + Razor Pages**: Conforms to technology selection
- ✅ **Bootstrap frontend**: Use Bootstrap styles
- ✅ **Function length ≤50 lines**: All methods meet requirements
- ✅ **Nesting ≤3 layers**: Code structure is concise

### 5.3 Security Constraint Compliance
- ✅ **Environment variable authentication**: Docker configuration for admin credentials
- ✅ **Cookie security**: HttpOnly, Secure, SameSite=Strict
- ✅ **HMACSHA256 signature**: Anti-tampering mechanism
- ✅ **Session timeout**: Configurable 5-1440 minutes

### 5.4 MVP Optimization Results
- ✅ **Remove complex service layer**: Implement logic directly in PageModel
- ✅ **Simplify middleware**: Keep only necessary authentication functions
- ✅ **Unified Razor Pages**: All functions use page models
- ✅ **Conform to monolithic architecture**: Strictly prohibit frontend-backend separation

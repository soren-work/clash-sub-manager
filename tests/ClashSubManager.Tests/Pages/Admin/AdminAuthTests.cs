using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;
using ClashSubManager.Pages.Admin;
using ClashSubManager.Middleware;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class LoginTests
    {
        private readonly Mock<IStringLocalizer<LoginModel>> _mockLocalizer;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly LoginModel _loginModel;

        public LoginTests()
        {
            _mockLocalizer = new Mock<IStringLocalizer<LoginModel>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Set default configuration
            _mockConfiguration.Setup(c => c["AdminUsername"]).Returns("admin");
            _mockConfiguration.Setup(c => c["AdminPassword"]).Returns("password");
            _mockConfiguration.Setup(c => c["CookieSecretKey"]).Returns("test-key-32-characters-long");
            _mockConfiguration.Setup(c => c["SessionTimeoutMinutes"]).Returns("30");
            
            _loginModel = new LoginModel(_mockConfiguration.Object, _mockLocalizer.Object);
            
            _mockLocalizer.Setup(l => l["UsernameAndPasswordRequired"]).Returns(new LocalizedString("UsernameAndPasswordRequired", "Username and password are required"));
            _mockLocalizer.Setup(l => l["InvalidCredentials"]).Returns(new LocalizedString("InvalidCredentials", "Invalid username or password"));
        }

        [Fact]
        public void OnGet_ShouldReturnPage()
        {
            _loginModel.OnGet();
            
            Assert.Null(null);
        }

        [Fact]
        public void OnPost_WithEmptyCredentials_ShouldReturnPageWithError()
        {
            _loginModel.Username = "";
            _loginModel.Password = "";
            
            var result = _loginModel.OnPost();
            
            Assert.IsType<PageResult>(result);
            Assert.Equal("Username and password are required", _loginModel.ErrorMessage);
        }

        [Fact]
        public void OnPost_WithInvalidCredentials_ShouldReturnPageWithError()
        {
            _loginModel.Username = "wronguser";
            _loginModel.Password = "wrongpass";
            
            var result = _loginModel.OnPost();
            
            Assert.IsType<PageResult>(result);
            Assert.Equal("Invalid username or password", _loginModel.ErrorMessage);
        }

        [Fact]
        public void OnPost_WithValidCredentials_ShouldRedirectToIndex()
        {
            // Setup mock HttpContext and Response
            var httpContext = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            var cookies = new Mock<IResponseCookies>();
            
            response.Setup(r => r.Cookies).Returns(cookies.Object);
            httpContext.Setup(c => c.Response).Returns(response.Object);
            
            // Use reflection to set the PageContext
            var pageContext = new PageContext
            {
                HttpContext = httpContext.Object
            };
            typeof(PageModel).GetProperty("PageContext")?.SetValue(_loginModel, pageContext);
            
            _loginModel.Username = "admin";
            _loginModel.Password = "password";
            
            var result = _loginModel.OnPost();
            
            Assert.IsType<RedirectToPageResult>(result);
            var redirectResult = result as RedirectToPageResult;
            Assert.Equal("/Admin/Index", redirectResult.PageName);
        }
    }

    public class LogoutTests
    {
        private readonly Mock<IStringLocalizer<LogoutModel>> _mockLocalizer;
        private readonly LogoutModel _logoutModel;
        private readonly HttpContext _httpContext;

        public LogoutTests()
        {
            _mockLocalizer = new Mock<IStringLocalizer<LogoutModel>>();
            _httpContext = new DefaultHttpContext();
            _logoutModel = new LogoutModel(_mockLocalizer.Object)
            {
                PageContext = new PageContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Fact]
        public void OnPost_ShouldDeleteCookieAndRedirectToLogin()
        {
            var result = _logoutModel.OnPost();
            
            Assert.IsType<RedirectToPageResult>(result);
            var redirectResult = result as RedirectToPageResult;
            Assert.NotNull(redirectResult);
            Assert.Equal("/Admin/Login", redirectResult.PageName);
        }
    }

    public class AdminAuthMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AdminAuthMiddleware _middleware;
        private readonly HttpContext _httpContext;

        public AdminAuthMiddlewareTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["CookieSecretKey"]).Returns("test-key-32-characters-long");
            _middleware = new AdminAuthMiddleware(_mockNext.Object, _mockConfiguration.Object);
            _httpContext = new DefaultHttpContext();
        }

        [Fact]
        public async Task InvokeAsync_WithNonAdminPath_ShouldCallNext()
        {
            _httpContext.Request.Path = "/some-other-path";
            
            await _middleware.InvokeAsync(_httpContext);
            
            _mockNext.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithLoginPath_ShouldCallNext()
        {
            _httpContext.Request.Path = "/admin/login";
            
            await _middleware.InvokeAsync(_httpContext);
            
            _mockNext.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithLogoutPath_ShouldCallNext()
        {
            _httpContext.Request.Path = "/admin/logout";
            
            await _middleware.InvokeAsync(_httpContext);
            
            _mockNext.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithAdminPathAndNoCookie_ShouldRedirectToLogin()
        {
            _httpContext.Request.Path = "/admin";
            _httpContext.Response.Body = new MemoryStream();
            
            await _middleware.InvokeAsync(_httpContext);
            
            Assert.Equal(302, _httpContext.Response.StatusCode);
            Assert.Equal("/Admin/Login", _httpContext.Response.Headers["Location"]);
            _mockNext.Verify(next => next(_httpContext), Times.Never);
        }
    }
}

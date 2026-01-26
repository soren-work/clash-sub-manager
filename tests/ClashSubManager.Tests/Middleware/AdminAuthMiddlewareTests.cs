using ClashSubManager.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using System.Security.Cryptography;
using System.Text;

namespace ClashSubManager.Tests.Middleware
{
    /// <summary>
    /// AdminAuthMiddleware unit tests
    /// </summary>
    public class AdminAuthMiddlewareTests : IDisposable
    {
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly Mock<ILogger<AdminAuthMiddleware>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AdminAuthMiddleware _middleware;
        private readonly string _testHmacKey = "test-hmac-key-32-characters-minimum";

        public AdminAuthMiddlewareTests()
        {
            _nextMock = new Mock<RequestDelegate>();
            _loggerMock = new Mock<ILogger<AdminAuthMiddleware>>();
            _configurationMock = new Mock<IConfiguration>();
            
            // Setup mock configuration
            _configurationMock.Setup(c => c["CookieSecretKey"]).Returns(_testHmacKey);
            
            _middleware = new AdminAuthMiddleware(_nextMock.Object, _configurationMock.Object, NullLogger<AdminAuthMiddleware>.Instance);
        }

        public void Dispose()
        {
            // Cleanup resources
        }

        #region Constructor Tests

        [Fact]
        public void AdminAuthMiddleware_Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CookieSecretKey"]).Returns(_testHmacKey);
            var middleware = new AdminAuthMiddleware(_nextMock.Object, configMock.Object, NullLogger<AdminAuthMiddleware>.Instance);

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public void AdminAuthMiddleware_Constructor_WithNullConfiguration_UsesDefaultKey()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CookieSecretKey"]).Returns((string?)null);

            // Act
            var middleware = new AdminAuthMiddleware(_nextMock.Object, configMock.Object, NullLogger<AdminAuthMiddleware>.Instance);

            // Assert
            Assert.NotNull(middleware);
        }

        #endregion

        #region InvokeAsync Tests

        [Fact]
        public async Task InvokeAsync_NonAdminPath_CallsNext()
        {
            // Arrange
            var context = CreateHttpContext("/api/health");
            _nextMock.Setup(x => x(context)).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AdminLoginPath_CallsNext()
        {
            // Arrange
            var context = CreateHttpContext("/admin/login");
            _nextMock.Setup(x => x(context)).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AdminLogoutPath_CallsNext()
        {
            // Arrange
            var context = CreateHttpContext("/admin/logout");
            _nextMock.Setup(x => x(context)).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AdminPathWithNoCookie_RedirectsToLogin()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(x => x["AdminSession"]).Returns((string?)null);
            
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(x => x.Redirect("/Admin/Login")).Verifiable();
            
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Path).Returns("/admin/index");
            httpContextMock.Setup(x => x.Request.Cookies).Returns(cookies.Object);
            httpContextMock.Setup(x => x.Response).Returns(responseMock.Object);

            // Act
            await _middleware.InvokeAsync(httpContextMock.Object);

            // Assert
            responseMock.Verify(x => x.Redirect("/Admin/Login"), Times.Once);
            _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_AdminPathWithInvalidCookie_RedirectsToLogin()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(x => x["AdminSession"]).Returns("invalid-cookie-format");
            
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(x => x.Redirect("/Admin/Login")).Verifiable();
            
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Path).Returns("/admin/index");
            httpContextMock.Setup(x => x.Request.Cookies).Returns(cookies.Object);
            httpContextMock.Setup(x => x.Response).Returns(responseMock.Object);

            // Act
            await _middleware.InvokeAsync(httpContextMock.Object);

            // Assert
            responseMock.Verify(x => x.Redirect("/Admin/Login"), Times.Once);
            _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_AdminPathWithValidCookie_CallsNext()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            var validCookie = CreateValidSessionCookie();
            cookies.Setup(x => x["AdminSession"]).Returns(validCookie);
            
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Path).Returns("/admin/index");
            httpContextMock.Setup(x => x.Request.Cookies).Returns(cookies.Object);
            
            _nextMock.Setup(x => x(httpContextMock.Object)).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _middleware.InvokeAsync(httpContextMock.Object);

            // Assert
            _nextMock.Verify(x => x(httpContextMock.Object), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AdminPathCaseInsensitive_WorksCorrectly()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(x => x["AdminSession"]).Returns((string?)null);
            
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(x => x.Redirect("/Admin/Login")).Verifiable();
            
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Path).Returns("/ADMIN/index");
            httpContextMock.Setup(x => x.Request.Cookies).Returns(cookies.Object);
            httpContextMock.Setup(x => x.Response).Returns(responseMock.Object);

            // Act
            await _middleware.InvokeAsync(httpContextMock.Object);

            // Assert
            responseMock.Verify(x => x.Redirect("/Admin/Login"), Times.Once);
            _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_NullPath_HandlesGracefully()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = PathString.Empty;
            _nextMock.Setup(x => x(context)).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region ValidateSessionCookie Tests

        [Fact]
        public void ValidateSessionCookie_NullCookie_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();

            // Act
            var result = TestValidateSessionCookie(middleware, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_EmptyCookie_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();

            // Act
            var result = TestValidateSessionCookie(middleware, "");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_InvalidFormat_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();

            // Act
            var result = TestValidateSessionCookie(middleware, "invalid-format");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_InvalidSessionId_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();
            var timestamp = DateTime.UtcNow.AddMinutes(30).ToString("yyyyMMddHHmmss");

            // Act
            var result = TestValidateSessionCookie(middleware, $"invalid-session-id:{timestamp}:signature");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_InvalidTimestamp_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();
            var sessionId = Guid.NewGuid().ToString("N");

            // Act
            var result = TestValidateSessionCookie(middleware, $"{sessionId}:invalid-timestamp:signature");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_ExpiredSession_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();
            var sessionId = Guid.NewGuid().ToString("N");
            // Create a clearly expired timestamp - use yesterday's time to ensure absolute expiration
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var expiredTimestamp = yesterday.ToString("yyyyMMddHHmmss");
            var signature = GenerateSignature(sessionId, expiredTimestamp, _testHmacKey);

            // Act
            var result = TestValidateSessionCookie(middleware, $"{sessionId}:{expiredTimestamp}:{signature}");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_InvalidSignature_ReturnsFalse()
        {
            // Arrange
            var middleware = CreateTestMiddleware();
            var sessionId = Guid.NewGuid().ToString("N");
            var timestamp = DateTime.UtcNow.AddMinutes(30).ToString("yyyyMMddHHmmss");

            // Act
            var result = TestValidateSessionCookie(middleware, $"{sessionId}:{timestamp}:invalid-signature");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSessionCookie_ValidCookie_ReturnsTrue()
        {
            // Arrange
            var middleware = CreateTestMiddleware();
            var validCookie = CreateValidSessionCookie();

            // Act
            var result = TestValidateSessionCookie(middleware, validCookie);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Helper Methods

        private HttpContext CreateHttpContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            return context;
        }

        private AdminAuthMiddleware CreateTestMiddleware()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CookieSecretKey"]).Returns(_testHmacKey);
            return new AdminAuthMiddleware(_nextMock.Object, configMock.Object, NullLogger<AdminAuthMiddleware>.Instance);
        }

        private bool TestValidateSessionCookie(AdminAuthMiddleware middleware, string? cookieValue)
        {
            // Use reflection to call private method for testing
            var method = typeof(AdminAuthMiddleware).GetMethod("TryValidateSessionCookie",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
                throw new InvalidOperationException("Failed to find TryValidateSessionCookie method via reflection.");

            var args = new object?[] { cookieValue, string.Empty };
            var result = method.Invoke(middleware, args);
            return result is bool value && value;
        }

        private string CreateValidSessionCookie()
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var timestamp = DateTime.UtcNow.AddMinutes(30).ToString("yyyyMMddHHmmss");
            var signature = GenerateSignature(sessionId, timestamp, _testHmacKey);
            
            return $"{sessionId}:{timestamp}:{signature}";
        }

        private string GenerateSignature(string sessionId, string timestamp, string hmacKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacKey));
            var signatureData = $"{sessionId}|{timestamp}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}

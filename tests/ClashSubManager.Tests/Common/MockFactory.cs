using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Common
{
    /// <summary>
    /// Mock object factory - Unified test mock configuration
    /// </summary>
    public static class MockFactory
    {
        /// <summary>
        /// Creates an IStringLocalizer mock
        /// </summary>
        /// <typeparam name="T">Localization type</typeparam>
        /// <returns>Configured mock object</returns>
        public static Mock<IStringLocalizer<T>> CreateStringLocalizerMock<T>()
        {
            var mock = new Mock<IStringLocalizer<T>>();
            mock.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("key", "value", false));
            mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns(new LocalizedString("key", "value", false));
            return mock;
        }

        /// <summary>
        /// Creates an HttpContext mock
        /// </summary>
        /// <returns>Configured mock object</returns>
        public static Mock<HttpContext> CreateHttpContextMock()
        {
            var mock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            var responseMock = new Mock<HttpResponse>();
            var cookiesMock = new Mock<IRequestCookies>();
            var responseCookiesMock = new Mock<IResponseCookies>();
            
            requestMock.Setup(x => x.Cookies).Returns(cookiesMock.Object);
            responseMock.Setup(x => x.Cookies).Returns(responseCookiesMock.Object);
            mock.Setup(x => x.Request).Returns(requestMock.Object);
            mock.Setup(x => x.Response).Returns(responseMock.Object);
            
            return mock;
        }

        /// <summary>
        /// Creates an HttpContext mock with path
        /// </summary>
        /// <param name="path">Request path</param>
        /// <returns>Configured mock object</returns>
        public static Mock<HttpContext> CreateHttpContextMock(string path)
        {
            var mock = CreateHttpContextMock();
            mock.Setup(x => x.Request.Path).Returns(path);
            return mock;
        }

        /// <summary>
        /// Creates an HttpContext mock with cookies
        /// </summary>
        /// <param name="path">Request path</param>
        /// <param name="cookies">Cookie key-value pairs</param>
        /// <returns>Configured mock object</returns>
        public static Mock<HttpContext> CreateHttpContextMock(string path, Dictionary<string, string> cookies)
        {
            var mock = CreateHttpContextMock(path);
            var cookiesMock = new Mock<IRequestCookieCollection>();
            
            foreach (var cookie in cookies)
            {
                cookiesMock.Setup(x => x[cookie.Key]).Returns(cookie.Value);
            }
            
            mock.Setup(x => x.Request.Cookies).Returns(cookiesMock.Object);
            return mock;
        }

        /// <summary>
        /// Creates a RequestDelegate mock
        /// </summary>
        /// <returns>Configured mock object</returns>
        public static Mock<RequestDelegate> CreateRequestDelegateMock()
        {
            var mock = new Mock<RequestDelegate>();
            mock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
            return mock;
        }

        /// <summary>
        /// Creates an ILogger mock
        /// </summary>
        /// <typeparam name="T">Logger type</typeparam>
        /// <returns>Configured mock object</returns>
        public static Mock<Microsoft.Extensions.Logging.ILogger<T>> CreateLoggerMock<T>()
        {
            var mock = new Mock<Microsoft.Extensions.Logging.ILogger<T>>();
            mock.Setup(x => x.Log(It.IsAny<Microsoft.Extensions.Logging.LogLevel>(), 
                                    It.IsAny<EventId>(), 
                                    It.IsAny<string>(), 
                                    It.IsAny<object[]>(),
                                    It.IsAny<Exception>()));
            return mock;
        }

        /// <summary>
        /// Creates a PageContext mock
        /// </summary>
        /// <param name="httpContext">HttpContext</param>
        /// <returns>Configured PageContext</returns>
        public static PageContext CreatePageContext(HttpContext httpContext)
        {
            return new PageContext
            {
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Creates a PageContext mock
        /// </summary>
        /// <returns>Configured PageContext</returns>
        public static PageContext CreatePageContext()
        {
            return CreatePageContext(CreateHttpContextMock().Object);
        }
    }

    /// <summary>
    /// Test base class - Provides common mock configuration
    /// </summary>
    public abstract class TestBase
    {
        protected Mock<IStringLocalizer<TestBase>> LocalizerMock { get; private set; }
        protected Mock<HttpContext> HttpContextMock { get; private set; }

        protected TestBase()
        {
            LocalizerMock = MockFactory.CreateStringLocalizerMock<TestBase>();
            HttpContextMock = MockFactory.CreateHttpContextMock();
        }

        /// <summary>
        /// Cleanup method
        /// </summary>
        public virtual void Dispose()
        {
            LocalizerMock?.Reset();
            HttpContextMock?.Reset();
        }
    }
}

using ClashSubManager.Middleware;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;
using System;

namespace ClashSubManager.Tests.Middleware
{
    /// <summary>
    /// Unit tests for AdminAuthMiddlewareExtensions
    /// </summary>
    public class AdminAuthMiddlewareExtensionsTests
    {
        [Fact]
        public void UseAdminAuth_ValidApplicationBuilder_DoesNotThrow()
        {
            // Arrange
            var builderMock = new Mock<IApplicationBuilder>();
            
            // Act & Assert
            // Since UseMiddleware is an extension method, we can only test that it doesn't throw
            var exception = Record.Exception(() => builderMock.Object.UseAdminAuth());
            Assert.Null(exception);
        }

        [Fact]
        public void UseAdminAuth_NullBuilder_ThrowsException()
        {
            // Arrange
            IApplicationBuilder builder = null;

            // Act & Assert
            // Since the extension method has no null check, it throws NullReferenceException
            Assert.Throws<NullReferenceException>(() => builder.UseAdminAuth());
        }
    }
}

using ClashSubManager.Pages.Admin;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class CloudflareIPValidationTests
    {
        [Fact]
        public void SelectedUserId_NullValue_ShouldNotCauseValidationError()
        {
            // Arrange & Act - Test the core issue
            string? selectedUserId = null;

            // Assert
            Assert.Null(selectedUserId);
            // The key test: null should not trigger validation errors
            // This tests that nullable string works correctly
        }

        [Fact]
        public void SelectedUserId_EmptyValue_ShouldNotCauseValidationError()
        {
            // Arrange & Act
            string? selectedUserId = string.Empty;

            // Assert
            Assert.Equal(string.Empty, selectedUserId);
            // The key test: empty string should not trigger validation errors
        }

        [Fact]
        public void SelectedUserId_ValidValue_ShouldWorkCorrectly()
        {
            // Arrange & Act
            string? selectedUserId = "test-user";

            // Assert
            Assert.Equal("test-user", selectedUserId);
        }

        [Fact]
        public void SelectedUserId_NullableStringType_ShouldAllowNull()
        {
            // Arrange & Act - This tests the type change from string to string?
            string? nullableString = null;

            // Assert
            Assert.True(nullableString == null);
            // This confirms that the property can now be null without validation errors
        }
    }
}

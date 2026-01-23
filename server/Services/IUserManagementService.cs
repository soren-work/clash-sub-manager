using ClashSubManager.Models;

namespace ClashSubManager.Services
{
    /// <summary>
    /// User management service interface
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Record user access
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Operation result</returns>
        Task<bool> RecordUserAccessAsync(string userId);

        /// <summary>
        /// Get all users list
        /// </summary>
        /// <returns>User ID list</returns>
        Task<List<string>> GetAllUsersAsync();

        /// <summary>
        /// Check if user exists
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Whether user exists</returns>
        Task<bool> UserExistsAsync(string userId);

        /// <summary>
        /// Delete user record
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Operation result</returns>
        Task<bool> DeleteUserAsync(string userId);

        /// <summary>
        /// Get user subscription URL
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Subscription URL</returns>
        Task<string> GetUserSubscriptionUrlAsync(string userId);
    }
}

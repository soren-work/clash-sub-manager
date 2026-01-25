using ClashSubManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClashSubManager.Services
{
    /// <summary>
    /// User management service implementation
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<UserManagementService> _logger;
        private readonly string _dataPath;
        private readonly string _usersFilePath;

        public UserManagementService(
            IConfigurationService configurationService,
            ILogger<UserManagementService> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
            _dataPath = _configurationService.GetDataPath();
            _usersFilePath = Path.Combine(_dataPath, "users.txt");
        }

        /// <summary>
        /// Records user access
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Operation result</returns>
        public async Task<bool> RecordUserAccessAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Attempted to record empty user ID");
                    return false;
                }

                // Validate user ID format
                if (!IsValidUserId(userId))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                    return false;
                }

                // Ensure data directory exists
                Directory.CreateDirectory(_dataPath);

                // Read existing user list
                var existingUsers = await ReadUsersFromFileAsync();
                
                // If user does not exist, add to list
                if (!existingUsers.Contains(userId))
                {
                    existingUsers.Add(userId);
                    await WriteUsersToFileAsync(existingUsers);
                    _logger.LogInformation("New user recorded: {UserId}", userId);
                }
                else
                {
                    _logger.LogDebug("Existing user accessed: {UserId}", userId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording user access: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets all users list
        /// </summary>
        /// <returns>User ID list</returns>
        public async Task<List<string>> GetAllUsersAsync()
        {
            try
            {
                var users = await ReadUsersFromFileAsync();
                _logger.LogDebug("Retrieved {Count} users from file", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if user exists
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Whether user exists</returns>
        public async Task<bool> UserExistsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                var users = await ReadUsersFromFileAsync();
                return users.Contains(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Deletes user record
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Operation result</returns>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Attempted to delete empty user ID");
                    return false;
                }

                var users = await ReadUsersFromFileAsync();
                if (users.Remove(userId))
                {
                    await WriteUsersToFileAsync(users);
                    
                    // Delete user dedicated directory
                    var userDir = Path.Combine(_dataPath, userId);
                    if (Directory.Exists(userDir))
                    {
                        Directory.Delete(userDir, true);
                        _logger.LogInformation("User directory deleted: {UserDir}", userDir);
                    }

                    _logger.LogInformation("User deleted: {UserId}", userId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("User not found for deletion: {UserId}", userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets user subscription URL
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Subscription URL</returns>
        public async Task<string> GetUserSubscriptionUrlAsync(string userId)
        {
            try
            {
                var urlTemplate = _configurationService.GetSubscriptionUrlTemplate();
                
                if (string.IsNullOrWhiteSpace(urlTemplate))
                {
                    _logger.LogError("Subscription URL template not configured");
                    return string.Empty;
                }

                // Replace {userId} placeholder
                var subscriptionUrl = urlTemplate.Replace("{userId}", userId, StringComparison.OrdinalIgnoreCase);
                
                _logger.LogDebug("Generated subscription URL for user {UserId}: {Url}", userId, subscriptionUrl);
                return subscriptionUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating subscription URL for user: {UserId}", userId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads user list from file
        /// </summary>
        /// <returns>User ID list</returns>
        private async Task<List<string>> ReadUsersFromFileAsync()
        {
            var users = new List<string>();

            if (!File.Exists(_usersFilePath))
            {
                return users;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(_usersFilePath);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine) && IsValidUserId(trimmedLine))
                    {
                        users.Add(trimmedLine);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading users file: {FilePath}", _usersFilePath);
            }

            return users.Distinct().OrderBy(u => u).ToList();
        }

        /// <summary>
        /// Writes user list to file
        /// </summary>
        /// <param name="users">User ID list</param>
        private async Task WriteUsersToFileAsync(List<string> users)
        {
            try
            {
                // Use temporary file mode to ensure atomic write
                var tempFilePath = _usersFilePath + ".tmp";
                
                var distinctUsers = users.Distinct().OrderBy(u => u).ToList();
                await File.WriteAllLinesAsync(tempFilePath, distinctUsers);
                
                // Atomic replacement
                if (File.Exists(_usersFilePath))
                {
                    File.Replace(tempFilePath, _usersFilePath, null);
                }
                else
                {
                    File.Move(tempFilePath, _usersFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing users file: {FilePath}", _usersFilePath);
                throw;
            }
        }

        /// <summary>
        /// Validates user ID format
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Whether valid</returns>
        private bool IsValidUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            if (userId.Length < 1 || userId.Length > 64)
                return false;

            return userId.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
        }
    }
}

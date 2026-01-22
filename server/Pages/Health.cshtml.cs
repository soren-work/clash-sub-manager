using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClashSubManager.Pages
{
    /// <summary>
    /// Health check endpoint for Docker container monitoring
    /// </summary>
    public class HealthModel : PageModel
    {
        /// <summary>
        /// GET /health - Health check endpoint
        /// </summary>
        /// <returns>Health status response</returns>
        public IActionResult OnGet()
        {
            try
            {
                // Check basic system health
                var healthStatus = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    uptime = GetUptime(),
                    memory = GetMemoryUsage()
                };

                return new JsonResult(healthStatus);
            }
            catch (Exception ex)
            {
                // Return unhealthy status if any exception occurs
                var errorStatus = new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                };

                return new JsonResult(errorStatus) { StatusCode = 503 };
            }
        }

        /// <summary>
        /// Get application uptime
        /// </summary>
        /// <returns>Uptime in seconds</returns>
        private long GetUptime()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds;
        }

        /// <summary>
        /// Get memory usage in MB
        /// </summary>
        /// <returns>Memory usage in MB</returns>
        private double GetMemoryUsage()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2);
        }
    }
}

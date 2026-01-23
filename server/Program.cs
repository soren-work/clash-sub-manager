using Microsoft.AspNetCore.Localization;
using System.Globalization;
using ClashSubManager.Services;
using ClashSubManager.Middleware;

// Check if configuration file contains required settings
bool HasRequiredConfigurations(string configPath)
{
    try
    {
        if (!File.Exists(configPath))
            return false;

        var json = File.ReadAllText(configPath);
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        return config != null && 
               config.ContainsKey("CookieSecretKey") && 
               config.ContainsKey("SessionTimeoutMinutes") &&
               config.ContainsKey("DataPath");
    }
    catch
    {
        return false;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Get environment type and configure prioritized configuration loading
var environmentDetector = new EnvironmentDetector();
var environmentType = environmentDetector.GetEnvironmentType();

// Check if this is the first startup (appsettings.json doesn't exist or lacks key configurations)
var appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
var isFirstStart = !File.Exists(appSettingsPath) || !HasRequiredConfigurations(appSettingsPath);

// Configure prioritized configuration loading
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentType}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// If this is the first startup, generate default configuration
if (isFirstStart)
{
    var validator = new ConfigurationValidator();
    try
    {
        await validator.WriteDefaultConfigurationAsync(builder.Configuration, appSettingsPath);
        Console.WriteLine("Default configuration has been generated successfully.");
        
        // Recreate builder to reload configuration
        builder = WebApplication.CreateBuilder(args);
        
        // Reconfigure prioritized configuration loading
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentType}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to generate default configuration: {ex.Message}");
    }
}

// Validate configuration
var finalValidator = new ConfigurationValidator();
try
{
    finalValidator.Validate(builder.Configuration);
    Console.WriteLine($"Configuration validation passed for environment: {environmentType}");
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration validation failed: {string.Join(", ", ex.ValidationErrors)}");
    Console.WriteLine("Please set the required environment variables or update the configuration file:");
    Console.WriteLine("- ADMIN_USERNAME: Administrator username");
    Console.WriteLine("- ADMIN_PASSWORD: Administrator password");
    Console.WriteLine("- COOKIE_SECRET_KEY: Cookie secret key (minimum 32 characters)");
    throw;
}

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Register cross-platform configuration services
builder.Services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
builder.Services.AddSingleton<IPathResolver, PathResolver>();
builder.Services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
builder.Services.AddSingleton<IConfigurationService, PlatformConfigurationService>();

// Register custom services
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<ValidationService>();
builder.Services.AddSingleton<IUserManagementService, UserManagementService>();
builder.Services.AddTransient<SubscriptionService>();

// Configure localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("zh-CN") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAdminAuth();
app.MapRazorPages();

app.Run();

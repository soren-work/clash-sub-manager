using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
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
    catch (Exception ex)
    {
        using var tempLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var tempLogger = tempLoggerFactory.CreateLogger<Program>();
        tempLogger.LogError(ex, "Failed to read or parse configuration file: {ConfigPath}", configPath);
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

// Override log level from environment variable if specified
var logLevelFromEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
if (!string.IsNullOrWhiteSpace(logLevelFromEnv) && string.IsNullOrWhiteSpace(builder.Configuration["Logging:LogLevel:Default"]))
{
    builder.Configuration["Logging:LogLevel:Default"] = logLevelFromEnv;
}

// If this is the first startup, generate default configuration
if (isFirstStart)
{
    using var tempLoggerFactoryForDefaultConfig = LoggerFactory.Create(logging => logging.AddConsole());
    var validatorLogger = tempLoggerFactoryForDefaultConfig.CreateLogger<ConfigurationValidator>();
    var validator = new ConfigurationValidator(validatorLogger);
    try
    {
        await validator.WriteDefaultConfigurationAsync(builder.Configuration, appSettingsPath);
        
        // Use ILogger to log success message
        using var tempLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var tempLogger = tempLoggerFactory.CreateLogger<Program>();
        tempLogger.LogInformation("Default configuration has been generated successfully.");
        
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
        using var tempLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var tempLogger = tempLoggerFactory.CreateLogger<Program>();
        tempLogger.LogError(ex, "Failed to generate default configuration");
    }
}

// Validate configuration
using var tempLoggerFactoryForValidation = LoggerFactory.Create(logging => logging.AddConsole());
var finalValidatorLogger = tempLoggerFactoryForValidation.CreateLogger<ConfigurationValidator>();
var finalValidator = new ConfigurationValidator(finalValidatorLogger);
try
{
    finalValidator.Validate(builder.Configuration);
    
    // Note: Will log validation success after app is built
}
catch (ConfigurationException ex)
{
    // Create a temporary logger for error logging
    using var tempLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
    var tempLogger = tempLoggerFactory.CreateLogger<Program>();
    tempLogger.LogError(ex, "Configuration validation failed: {ValidationErrors}", string.Join(", ", ex.ValidationErrors));
    tempLogger.LogInformation("Please set the required environment variables or update the configuration file:");
    tempLogger.LogInformation("- ADMIN_USERNAME: Administrator username");
    tempLogger.LogInformation("- ADMIN_PASSWORD: Administrator password");
    tempLogger.LogInformation("- COOKIE_SECRET_KEY: Cookie secret key (minimum 32 characters)");
    throw;
}

// Add services to the container
// IMPORTANT: AddLocalization must be called before AddRazorPages
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// Register HttpClient for services that need it
builder.Services.AddHttpClient();

// Register cross-platform configuration services
builder.Services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
builder.Services.AddSingleton<IPathResolver, PathResolver>();
builder.Services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
builder.Services.AddSingleton<IConfigurationService, PlatformConfigurationService>();
builder.Services.AddSingleton<IFileLockProvider, FileLockProvider>();

builder.Services.AddSingleton<INodeNamingTemplateService, NodeNamingTemplateService>();
builder.Services.AddSingleton<CloudflareIPParserService>();

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
    
    // Add CookieRequestCultureProvider to support language switching via cookie
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
    {
        CookieName = CookieRequestCultureProvider.DefaultCookieName
    });
});

var app = builder.Build();

// Log configuration validation success
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Configuration validation passed for environment: {EnvironmentType}", environmentType);

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

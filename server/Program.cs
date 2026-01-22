using Microsoft.AspNetCore.Localization;
using System.Globalization;
using ClashSubManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Register custom services
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<ValidationService>();
builder.Services.AddSingleton<ConfigurationService>();
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
app.MapRazorPages();

app.Run();

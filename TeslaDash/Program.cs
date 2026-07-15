using Microsoft.AspNetCore.DataProtection;
using TeslaDash.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<TeslaDash.Services.IVehicleDashboardService, TeslaDash.Services.DemoVehicleDashboardService>();
builder.Services.AddDataProtection()
    .SetApplicationName("TeslaDash")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["Tesla:DataProtectionPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys")));
builder.Services.Configure<TeslaOptions>(builder.Configuration.GetSection(TeslaOptions.SectionName));
builder.Services.AddSingleton<ITeslaTokenStore, ProtectedFileTeslaTokenStore>();
builder.Services.AddHttpClient<TeslaOAuthClient>();
builder.Services.AddHttpClient<TeslaFleetClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

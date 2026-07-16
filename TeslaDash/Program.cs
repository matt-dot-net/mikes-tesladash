using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TeslaDash.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Account/SignIn");
    options.Conventions.AllowAnonymousToPage("/Account/SignedOut");
});
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "TeslaDash.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddOpenIdConnect(options =>
    {
        var microsoft = builder.Configuration.GetSection("Authentication:Microsoft");
        options.Authority = $"{microsoft["Instance"]?.TrimEnd('/')}/{microsoft["TenantId"]}/v2.0";
        options.ClientId = microsoft["ClientId"];
        options.ClientSecret = microsoft["ClientSecret"];
        options.CallbackPath = microsoft["CallbackPath"] ?? "/signin-oidc";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "roles"
        };
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });
builder.Services.AddSingleton<TeslaDash.Services.IVehicleDashboardService, TeslaDash.Services.DemoVehicleDashboardService>();
builder.Services.AddDataProtection()
    .SetApplicationName("TeslaDash")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["Tesla:DataProtectionPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys")));
builder.Services.Configure<TeslaOptions>(builder.Configuration.GetSection(TeslaOptions.SectionName));
builder.Services.AddSingleton<ITeslaTokenStore, ProtectedFileTeslaTokenStore>();
builder.Services.AddSingleton<TeslaPublicKeyProvider>();
builder.Services.AddHttpClient<TeslaOAuthClient>();
builder.Services.AddHttpClient<TeslaFleetClient>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/.well-known/appspecific/com.tesla.3p.public-key.pem", (TeslaPublicKeyProvider keyProvider) =>
{
    var publicKey = keyProvider.GetPublicKeyPem();
    return publicKey is null
        ? Results.NotFound("Tesla private key is not configured.")
        : Results.Text(publicKey, "application/x-pem-file");
}).AllowAnonymous();

app.Run();

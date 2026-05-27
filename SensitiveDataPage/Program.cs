using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SensitiveDataPage.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IEncrypt, Encrypt>();
builder.Services.AddSingleton<IDecrypt, Decrypt>();
builder.Services.AddScoped<IAuditMechanism, AuditMechanism>();

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConn))
{
    throw new InvalidOperationException(
        "Configuration error: 'DefaultConnection' is not set. " +
        "Set it in appsettings.json, appsettings.Development.json, user-secrets, launchSettings.json, or environment variables."
    );
}

builder.Services.AddDbContext<SensitiveDataPage.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(defaultConn));

builder.Services.AddHostedService<DailyCountResetMechanism>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        "frame-src https://www.google.com/recaptcha/ https://recaptcha.google.com/recaptcha/; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self';";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (Microsoft.AspNetCore.Http.HttpContext ctx) =>
{
    ctx.Response.Redirect("/Login", permanent: false);
    return Task.CompletedTask;
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
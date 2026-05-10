using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SensitiveDataPage.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
//Delay is needed 
builder.Services.AddTransient<IEmailSender, EmailSender>();
await Task.Delay(500);
builder.Services.AddSingleton<IEncrypt, Encrypt>();
await Task.Delay(500);
builder.Services.AddSingleton<IDecrypt, Decrypt>();
await Task.Delay(500);
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
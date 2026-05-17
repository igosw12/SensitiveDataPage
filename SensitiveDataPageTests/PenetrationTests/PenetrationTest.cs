using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace SensitiveDataPageTests.PenetrationTests
{
    public class PenetrationTest : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private readonly BrowserFixture _browserFixture;
        private IBrowserContext _context = null!;
        private IPage _page = null!;
        private string? Email { get; set; }
        private string? Password { get; set; }

        public PenetrationTest(BrowserFixture browserFixture)
        {
            _browserFixture = browserFixture;
        }

        public async Task InitializeAsync()
        {
            await _browserFixture.InitializeAsync();
            _context = await _browserFixture.CreateBrowserContextAsync();
            _page = await _context.NewPageAsync();

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();

            Email = configuration["LoginStrings:Email"];
            Password = configuration["LoginStrings:Password"];
        }

        //Unauthorized access
        [Theory]
        [InlineData("/Dashboard")]
        public async Task UnauthenticatedAccess_ProtectedPage_RedirectsToLogin(string protectedPath)
        {
            //Act
            await _page.GotoAsync(protectedPath);

            //Assert
            await Assertions.Expect(_page).Not.ToHaveURLAsync(new Regex(Regex.Escape(protectedPath), RegexOptions.IgnoreCase));
        }

        //SQL injection on login
        [Theory]
        [InlineData("' OR '1'='1", "' OR '1'='1' --")]
        [InlineData("admin'--", "anything")]
        [InlineData("\" OR \"\"=\"", "\" OR \"\"=\"")]
        public async Task SqlInjection_LoginForm_DoesNotAuthenticate(string injectedEmail, string injectedPassword)
        {
            //Arrange
            await _page.GotoAsync("/login");

            //Act
            await _page.FillAsync("#Input_Email", injectedEmail);
            await _page.FillAsync("#Input_Password", injectedPassword);
            await _page.Locator("[data-i18n='login.btn']").ClickAsync();

            //Assert
            await Assertions.Expect(_page).Not.ToHaveURLAsync(new Regex("/dashboard", RegexOptions.IgnoreCase));
        }

        //XSS on login form
        [Theory]
        [InlineData("<script>alert('xss')</script>", "password")]
        [InlineData("<img src=x onerror=alert(1)>", "password")]
        [InlineData("javascript:alert(1)", "password")]
        public async Task XssPayload_LoginForm_IsNotExecuted(string xssPayload, string password)
        {
            //Arrange
            await _page.GotoAsync("/login");
            var dialogTriggered = false;
            _page.Dialog += (_, _) => dialogTriggered = true;

            //Act
            await _page.FillAsync("#Input_Email", xssPayload);
            await _page.FillAsync("#Input_Password", password);
            await _page.Locator("[data-i18n='login.btn']").ClickAsync();
            await _page.WaitForTimeoutAsync(500);

            //Assert
            Assert.False(dialogTriggered, "XSS payload triggered a dialog – potential XSS vulnerability.");
        }

        //XSS on registration form
        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<img src=x onerror=alert(1)>")]
        public async Task XssPayload_RegisterForm_IsNotExecuted(string xssPayload)
        {
            //Arrange
            await _page.GotoAsync("/Register");
            var dialogTriggered = false;
            _page.Dialog += (_, _) => dialogTriggered = true;
             
            //Act
            await _page.FillAsync("#Input_Email", xssPayload);
            await _page.FillAsync("#Input_Password", "Test123!");
            await _page.FillAsync("#Input_ConfirmPassword", "Test123!");
            await _page.WaitForTimeoutAsync(500);

            //Assert
            Assert.False(dialogTriggered, "XSS payload triggered a dialog on the registration page.");
        }

        //Brute-force protection
        [Fact]
        public async Task BruteForce_RepeatedFailedLogins_DoesNotGrantAccess()
        {
            //Arrange
            await _page.GotoAsync("/login");
            const int attempts = 10;

            //Act
            for (var i = 0; i < attempts; i++)
            {
                await _page.FillAsync("#Input_Email", "attacker@example.com");
                await _page.FillAsync("#Input_Password", $"WrongPassword{i}");
                await _page.Locator("[data-i18n='login.btn']").ClickAsync();
                await _page.WaitForTimeoutAsync(200);
            }

            //Assert
            await Assertions.Expect(_page).Not.ToHaveURLAsync(new Regex("/dashboard", RegexOptions.IgnoreCase));
        }

        //Open redirect
        [Theory]
        [InlineData("/login?returnUrl=https://evil.com")]
        [InlineData("/login?ReturnUrl=//evil.com")]
        public async Task OpenRedirect_LoginPage_DoesNotRedirectToExternalSite(string urlWithRedirect)
        {
            //Arrange
            await _page.GotoAsync(urlWithRedirect);

            if (Email is null || Password is null)
                throw new ArgumentException("Error during Initialization");

            //Act
            await _page.FillAsync("#Input_Email", Email);
            await _page.FillAsync("#Input_Password", Password);
            await _page.Locator("[data-i18n='login.btn']").ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            //Assert
            var finalUrl = _page.Url;
            Assert.DoesNotContain("evil.com", finalUrl);
        }

        //Sensitive data exposure in HTTP response headers
        [Fact]
        public async Task ResponseHeaders_LoginPage_DoNotExposeServerDetails()
        {
            //Act
            var response = await _page.GotoAsync("/login");

            //Assert
            Assert.NotNull(response);
            var headers = response.Headers;

            Assert.False(headers.ContainsKey("x-powered-by"), "X-Powered-By header exposes server technology.");
            Assert.False(headers.ContainsKey("server") && headers["server"].Contains("Kestrel"),
                "Server header exposes web server details.");
        }

        //Forgot-password endpoint – no user enumeration
        [Theory]
        [InlineData("existing@example.com")]
        [InlineData("nonexistent@example.com")]
        public async Task ForgotPassword_AnyEmail_ShowsGenericMessage(string email)
        {
            //Arrange
            await _page.GotoAsync("/ForgotPassword");

            //Act
            await _page.FillAsync("input[type='email']", email);
            await _page.Locator("button[type='submit']").ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            //Assert
            var content = await _page.ContentAsync();
            Assert.DoesNotContain("does not exist", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("not found", content, StringComparison.OrdinalIgnoreCase);
        }

        //Clickjacking protection
        [Theory]
        [InlineData("/login")]
        [InlineData("/Register")]
        [InlineData("/ForgotPassword")]
        public async Task ClickjackingProtection_PublicPages_HaveXFrameOptionsHeader(string path)
        {
            //Act
            var response = await _page.GotoAsync(path);

            //Assert
            Assert.NotNull(response);
            var headers = response.Headers;

            var hasXFrameOptions = headers.ContainsKey("x-frame-options");
            var hasContentSecurityPolicy = headers.TryGetValue("content-security-policy", out var csp)
                                           && csp.Contains("frame-ancestors", StringComparison.OrdinalIgnoreCase);

            Assert.True(hasXFrameOptions || hasContentSecurityPolicy,
                $"Page '{path}' is missing clickjacking protection headers (X-Frame-Options or CSP frame-ancestors).");
        }

        public async Task DisposeAsync()
        {
            await _page.CloseAsync();
            await _context.DisposeAsync();
        }
    }
}

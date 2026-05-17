using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace SensitiveDataPageTests.FunctionalTests
{
    public class DashboardTest : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private readonly BrowserFixture _browserFixture;
        private IBrowserContext _context = null!;
        private IPage _page = null!;
        private string? Email { get; set; }
        private string? Password { get; set; }

        public DashboardTest(BrowserFixture browserFixture)
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

        [Fact]
        public async Task LoginTest_DashboardPageTest()
        {
            //Assert
            if (Email is null || Password is null)
            {
                throw new ArgumentException("Error during Initialization");
            }
            await _page.GotoAsync("/login");
            await _page.FillAsync("#Input_Email", Email);
            await _page.FillAsync("#Input_Password", Password);
            await _page.Locator("[data-i18n='login.btn']").ClickAsync();
            //Act
            var locator = _page.Locator(".tier1Data").First;
            //Assert
            await Assertions.Expect(locator).ToContainTextAsync("Confidential Data");
        }

        [Fact]
        public async Task TabTest_DashboardPageTest()
        {
            //Arrange
            await LoginTest_DashboardPageTest();
            var tabValueBefore = await _page.Locator("#tier-title").First.InnerTextAsync();

            //Act
            await _page.Locator("[data-i18n='dash.tier2']").ClickAsync();
            var tabValueAfter = await _page.Locator("#tier-title").First.InnerTextAsync();

            //Assert
            Assert.NotEqual(tabValueAfter, tabValueBefore);
            Assert.Equal("Sensitive Data", tabValueAfter);
        }

        [Theory]
        [InlineData("en", "Confidential Data")]
        [InlineData("pl", "Dane poufne")]
        public async Task ChangeLanuage_DashboardPageTest(string lang, string text)
        {
            //Arrange
            await LoginTest_DashboardPageTest();
            var mainText = await _page.Locator(".tier1Data").First.InnerTextAsync();

            //Act
            if (mainText == text)
            {
                if (lang == "en")
                {
                    await _page.Locator("[data-lang='pl']").ClickAsync();
                }
                else if (lang == "pl")
                {
                    await _page.Locator("[data-lang='en']").ClickAsync();
                }
                mainText = await _page.Locator(".tier1Data").First.InnerTextAsync();
            }
            if (mainText != text)
            {
                await _page.Locator($"[data-lang='{lang}']").ClickAsync();
            }

            mainText = await _page.Locator(".tier1Data").First.InnerTextAsync();

            //Assert
            Assert.Equal(mainText, text);
        }

        [Theory]
        [InlineData("dark", "rgb(18, 18, 18)")]
        [InlineData("light", "rgb(255, 255, 255)")]
        public async Task ChangeDisplayMode_DashboardPageTest(string mode, string rgb)
        {
            //Arrange
            await LoginTest_DashboardPageTest();
            var currentMode = await _page.EvaluateAsync<string>(" () => document.documentElement.getAttribute('data-theme')");

            //Act
            if (currentMode == mode)
            {
                if (mode == "dark")
                {
                    await _page.Locator("#toggle-displaymode").ClickAsync();
                }
                else if (mode == "light")
                {
                    await _page.Locator("#toggle-displaymode").ClickAsync();
                }
                currentMode = await _page.Locator("#toggle-displaymode").First.InnerTextAsync();
            }
            if (currentMode != mode)
            {
                await _page.Locator("#toggle-displaymode").ClickAsync();
            }

            var currentRGB = await _page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");

            //Assert
            Assert.Equal(currentRGB, rgb);
        }

        [Fact]
        public async Task AddData_DashboardPageTest()
        {
            //Arrange
            await LoginTest_DashboardPageTest();
            await _page.Locator(".tier1Data").ClickAsync();

            //Act
            await _page.Locator("#input-category").SelectOptionAsync("Account");
            await _page.FillAsync("[data-i18n='dash.entryName']", "FunctionalTestAccount");
            await _page.FillAsync("#field-login", "test@test.pl");
            await _page.FillAsync("#field-password", "Test123!");
            await _page.FillAsync("#field-website", "https://www.test.pl");
            await _page.Locator("[data-i18n='dash.addBtn']").ClickAsync();
            await _page.WaitForSelectorAsync("[data-type='FunctionalTestAccount']", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Attached
            });

            //Assert
            var data = await _page.Locator("[data-category='Account']").AllInnerTextsAsync();
            Assert.Contains(data, item => item.Contains("FunctionalTestAccount"));
        }

        [Fact]
        public async Task DeleteData_DashboardPageTest()
        {
            //Arrange
            await LoginTest_DashboardPageTest();
            await _page.Locator(".tier1Data").ClickAsync();
            var dataBefore = await _page.Locator("[data-category='Account']").AllInnerTextsAsync();

            //Act
            _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await _page.Locator("[data-type='FunctionalTestAccount']").Locator(".icon-btn.text-danger").ClickAsync();
            await _page.WaitForSelectorAsync("[data-type='FunctionalTestAccount']", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Detached
            });

            //Assert
            var dataAfter = await _page.Locator("[data-category='Account']").AllInnerTextsAsync();
            Assert.NotEqual(dataBefore, dataAfter);
            Assert.DoesNotContain(dataAfter, item => item.Contains("FunctionalTestAccount"));
        }

        public async Task DisposeAsync()
        {
            await _page.CloseAsync();
            await _context.DisposeAsync();
        }
    }
}

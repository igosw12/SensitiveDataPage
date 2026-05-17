using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace SensitiveDataPageTests.End2EndTests
{
    public class RegisterTest : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private readonly BrowserFixture _browserFixture;
        private IBrowserContext _context = null!;
        private IPage _page = null!;

        public RegisterTest(BrowserFixture browserFixture)
        {
            _browserFixture = browserFixture;
        }

        public async Task InitializeAsync()
        {
            await _browserFixture.InitializeAsync();
            _context = await _browserFixture.CreateBrowserContextAsync();
            _page = await _context.NewPageAsync();
        }

        [Fact]
        public async Task PageLoad_RegisterPageTest()
        {
            //Arrange
            await _page.GotoAsync("/Register");

            //Act
            var locator = _page.Locator(".auth-card-title").First;

            //Assert
            await Assertions.Expect(locator).ToContainTextAsync("Create a new account and protect your sensitive data");
        }

        [Theory]
        [InlineData("en", "← Create a new account and protect your sensitive data")]
        [InlineData("pl", "← Utwórz nowe konto i chroń swoje dane wrażliwe")]
        public async Task ChangeLanguage_RegisterPageTest(string lang, string text)
        {
            //Arrange
            await _page.GotoAsync("/Register");
            var mainText = await _page.Locator(".auth-card-title").First.InnerTextAsync();

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
                mainText = await _page.Locator(".auth-card-title").First.InnerTextAsync();
            }
            if (mainText != text)
            {
                await _page.Locator($"[data-lang='{lang}']").ClickAsync();
            }

            mainText = await _page.Locator(".auth-card-title").First.InnerTextAsync();

            //Assert
            Assert.Equal(mainText, text);
        }

        [Theory]
        [InlineData("dark", "rgb(18, 18, 18)")]
        [InlineData("light", "rgb(255, 255, 255)")]
        public async Task ChangeDisplayMode_RegisterPageTest(string mode, string rgb)
        {
            //Arrange
            await _page.GotoAsync("/Register");
            var currentMode = await _page.EvaluateAsync<string>("() => document.documentElement.getAttribute('data-theme')");

            //Act
            if (currentMode == mode)
            {
                if (mode == "dark")
                {
                    await _page.Locator(".toggle-slider").ClickAsync();
                }
                else if (mode == "light")
                {
                    await _page.Locator(".toggle-slider").ClickAsync();
                }
                currentMode = await _page.Locator(".toggle-slider").First.InnerTextAsync();
            }
            if (currentMode != mode)
            {
                await _page.Locator(".toggle-slider").ClickAsync();
            }

            var currentRGB = await _page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");

            //Assert
            Assert.Equal(currentRGB, rgb);
        }

        [Fact]
        public async Task BackToLoginPage()
        {
            //Arrange
            await _page.GotoAsync("/login");
            await _page.Locator(".newAccountBtn").ClickAsync();

            //Act
            await _page.Locator(".back-arrow").ClickAsync();

            //Assert
            var content = _page.ContentAsync();
            await Assertions.Expect(_page).ToHaveURLAsync("/login");
            Assert.NotNull(content);
        }

        public async Task DisposeAsync()
        {
            await _page.CloseAsync();
            await _context.DisposeAsync();
        }
    }
}

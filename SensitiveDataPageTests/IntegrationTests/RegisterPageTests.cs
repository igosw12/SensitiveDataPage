using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Pages;
using SensitiveDataPage.Services;

namespace SensitiveDataPageTests.IntegrationTests
{
    public class RegisterPageTests
    {
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly TestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;

        public RegisterPageTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _emailSenderMock = new Mock<IEmailSender>();
            _fixture = new TestFixture();
        }

        private RegisterModel CreateRegisterModel(HttpContext httpContext, string email, string password, string? recaptchaToken = "valid-token")
        {
            var model = new RegisterModel(_dbContext, _emailSenderMock.Object)
            {
                Input = new RegisterModel.InputModel
                {
                    Email = email,
                    Password = password,
                    CheckPassword = password,
                    DateOfBirth = new DateTime(1990, 1, 1)
                },
                RecaptchaToken = recaptchaToken,
                PageContext = new PageContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
                Url = _fixture.CreateMockedUrlHelper()
            };
            return model;
        }

        [Fact]
        public async Task RegisterPageTest_ValidData()
        {
            // Arrange
            var httpContext = _fixture.CreateMockedHttpContext();
            var model = CreateRegisterModel(httpContext, "newuser@test.com", "Test123!");

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
            Assert.True(await _dbContext.Users.AnyAsync(u => u.Email == "newuser@test.com"));
        }

        [Fact]
        public async Task RegisterPageTest_EmailAlreadyInUse()
        {
            // Arrange
            const string email = "existing@test.com";
            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword("Test123!"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var model = CreateRegisterModel(_fixture.CreateMockedHttpContext(), email, "Test123!");

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("reg.emailInUse", message);
        }

        [Fact]
        public async Task RegisterPageTest_MissingRecaptchaToken()
        {
            // Arrange
            var model = CreateRegisterModel(_fixture.CreateMockedHttpContext(), "user@test.com", "Test123!", recaptchaToken: null);

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("reg.recaptchaFail", message);
        }
    }
}

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
    public class ForgotPasswordPageTests
    {
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly TestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;

        public ForgotPasswordPageTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _emailSenderMock = new Mock<IEmailSender>();
            _fixture = new TestFixture();
        }

        private ForgotPasswordModel CreateForgotPasswordModel(HttpContext httpContext, string email)
        {
            var model = new ForgotPasswordModel(_dbContext, _emailSenderMock.Object)
            {
                Input = new ForgotPasswordModel.InputModel { Email = email },
                PageContext = new PageContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
                Url = _fixture.CreateMockedUrlHelper()
            };
            return model;
        }

        [Fact]
        public async Task ForgotPasswordPageTest_ValidEmail()
        {
            // Arrange
            const string email = "user@test.com";
            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword("Test123!"),
                IsVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var model = CreateForgotPasswordModel(_fixture.CreateMockedHttpContext(), email);

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
            Assert.True(await _dbContext.PasswordResetTokens.AnyAsync(t => t.User!.Email == email));
        }

        [Fact]
        public async Task ForgotPasswordPageTest_EmailNotFound()
        {
            // Arrange
            var model = CreateForgotPasswordModel(_fixture.CreateMockedHttpContext(), "notexist@test.com");

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("forgot.noAccount", message);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Pages;
using SensitiveDataPage.Services;


namespace SensitiveDataPageTests.IntegrationTests
{
    public class LoginPageTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly TestFixture _fixture;
        private readonly IAuditMechanism _auditMechanism;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _configurationMock;

        public LoginPageTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName: "TestDatabase").Options;
            _dbContext = new ApplicationDbContext(options);
            _configurationMock = new Mock<IConfiguration>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _configurationMock.Setup(c => c["EmailStrings:EmailAddress"]).Returns("test@example.com");
            _configurationMock.Setup(c => c["EmailStrings:EmailPassword"]).Returns("password");

            _auditMechanism = new AuditMechanism(_dbContext, _httpContextAccessorMock.Object);
            _emailSenderMock = new Mock<IEmailSender>();

            _fixture = new TestFixture();
        }

        private LoginModel CreateLoginModel(HttpContext httpContext, string email = "", string password = "")
        {
            var model = new LoginModel(_dbContext, _emailSenderMock.Object, _auditMechanism)
            {
                Input = new LoginModel.InputModel
                {
                    Email = email,
                    Password = password
                },
                PageContext = new PageContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
            return model;
        }

        [Theory]
        [InlineData("test@test.com", "Test123!")]
        public async Task LoginPageTest_ValidData(string email, string password)
        {
            //Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateMockedHttpContext();
            var loginModel = CreateLoginModel(httpContext, email, password);

            //Act
            var results = await loginModel.OnPostAsync();

            //Assert
            var jsonResult = Assert.IsType<JsonResult>(results);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
        }

        [Theory]
        [InlineData("test1@test.com", "Test123!")]
        [InlineData("test@test.com", "WrongPassword!")]
        [InlineData("test@test.com", "")]
        public async Task LoginPageTest_InvalidData(string email, string password)
        {
            //Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateMockedHttpContext();
            var loginModel = CreateLoginModel(httpContext, email, password);

            //Act
            var results = await loginModel.OnPostAsync();

            //Assert
            var jsonResult = Assert.IsType<JsonResult>(results);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.False(success);
        }

        [Fact]
        public async Task LoginPageTest_NotVerified()
        {
            // Arrange
            const string email = "unverified@test.com";
            const string password = "Test123!";

            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword(password),
                IsVerified = false,
                IsActive = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var loginModel = CreateLoginModel(_fixture.CreateMockedHttpContext(), email, password);

            // Act
            var result = await loginModel.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("login.notVerified", message);
        }

        [Fact]
        public async Task LoginPageTest_AccountLocked()
        {
            // Arrange
            const string email = "locked@test.com";
            const string password = "Test123!";

            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword(password),
                IsVerified = true,
                IsActive = true,
                TwoFactorEnabled = false,
                FailedLoginAttempts = 5,
                LockoutUntil = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var loginModel = CreateLoginModel(_fixture.CreateMockedHttpContext(), email, password);

            // Act
            var result = await loginModel.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("login.locked", message);
        }

        [Fact]
        public async Task LoginPageTest_DeletedAccount()
        {
            // Arrange
            const string email = "deleted@test.com";
            const string password = "Test123!";

            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword(password),
                IsVerified = true,
                IsActive = false,
                DeletedAt = DateTime.UtcNow.AddDays(-1),
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var loginModel = CreateLoginModel(_fixture.CreateMockedHttpContext(), email, password);

            // Act
            var result = await loginModel.OnPostAsync();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("login.accountDeleted", message);
        }

        internal async Task InitializeUser()
        {
            const string email = "test@test.com";
            const string password = "Test123!";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = TestFixture.HashPassword(password),
                IsVerified = true,
                IsActive = true,
                TwoFactorEnabled = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}

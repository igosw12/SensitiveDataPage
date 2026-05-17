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
using System.Text.Json;

namespace SensitiveDataPageTests.IntegrationTests
{
    public class DashboardPageTests
    {
        private readonly Mock<IEncrypt> _encryptMock;
        private readonly Mock<IDecrypt> _decryptMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly IAuditMechanism _auditMechanism;
        private readonly TestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;

        private readonly Guid _userId = Guid.NewGuid();
        private const string UserEmail = "dashboard@test.com";

        public DashboardPageTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _encryptMock = new Mock<IEncrypt>();
            _decryptMock = new Mock<IDecrypt>();
            _fixture = new TestFixture();

            _encryptMock.Setup(e => e.EncryptData(It.IsAny<string>()))
                .Returns(new EncryptedResult
                {
                    EncryptedData = [1, 2, 3],
                    EncryptionIV = [4, 5, 6],
                    EncryptionTag = [7, 8, 9]
                });

            _auditMechanism = new AuditMechanism(_dbContext, _httpContextAccessorMock.Object);
        }

        private DashboardModel CreateDashboardModel(HttpContext httpContext)
        {
            var model = new DashboardModel(_dbContext, _encryptMock.Object, _decryptMock.Object, _auditMechanism)
            {
                Email = UserEmail,
                PageContext = new PageContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
            return model;
        }

        private async Task InitializeUser()
        {
            if (!await _dbContext.Users.AnyAsync(u => u.Id == _userId))
            {
                _dbContext.Users.Add(new User
                {
                    Id = _userId,
                    Email = UserEmail,
                    PasswordHash = TestFixture.HashPassword("Test123!"),
                    IsVerified = true,
                    IsActive = true,
                    TwoFactorEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task DashboardPageTest_SaveData_Valid()
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);
            var request = new DashboardModel.SaveDataRequest
            {
                Tier = 1,
                Type = "Password",
                Category = "Work",
                Fields = new Dictionary<string, string> { { "username", "admin" } }
            };

            // Act
            var result = await model.OnPostSaveDataAsync(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
            Assert.True(await _dbContext.SensitiveData.AnyAsync(s => s.UserId == _userId));
        }

        [Fact]
        public async Task DashboardPageTest_SaveData_EmptyFields()
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);
            var request = new DashboardModel.SaveDataRequest
            {
                Tier = 1,
                Type = "",
                Category = "",
                Fields = new Dictionary<string, string>()
            };

            // Act
            var result = await model.OnPostSaveDataAsync(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("dash.nameCategoryRequired", message);
        }

        [Fact]
        public async Task DashboardPageTest_DeleteData_Valid()
        {
            // Arrange
            await InitializeUser();
            var recordId = Guid.NewGuid();
            _dbContext.SensitiveData.Add(new SensitiveData
            {
                Id = recordId,
                UserId = _userId,
                EncryptedData = [1],
                EncryptionIV = [2],
                EncryptionTag = [3],
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);

            // Act
            var result = await model.OnPostDeleteDataAsync(new DashboardModel.DeleteDataRequest { Id = recordId });

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
            var record = await _dbContext.SensitiveData.FindAsync(recordId);
            Assert.NotNull(record!.DeletedAt);
        }

        [Fact]
        public async Task DashboardPageTest_DeleteData_RecordNotFound()
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);

            // Act
            var result = await model.OnPostDeleteDataAsync(new DashboardModel.DeleteDataRequest { Id = Guid.NewGuid() });

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal("dash.recordNotFound", message);
        }

        [Fact]
        public async Task DashboardPageTest_ChangePassword_Valid()
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);
            var request = new DashboardModel.ChangePasswordRequest
            {
                OldPassword = "Test123!",
                NewPassword = "NewPass456!",
                ConfirmPassword = "NewPass456!"
            };

            // Act
            var result = await model.OnPostChangePasswordAsync(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
        }

        [Theory]
        [InlineData("WrongOld!", "NewPass456!", "NewPass456!", "dash.passwordIncorrect")]
        [InlineData("Test123!", "Test123!", "Test123!", "dash.passwordTheSame")]
        [InlineData("Test123!", "NewPass456!", "Different!", "dash.passwordMismatch")]
        [InlineData("", "NewPass456!", "NewPass456!", "dash.allFieldsRequired")]
        public async Task DashboardPageTest_ChangePassword_InvalidCases(string oldPassword, string newPassword, string confirmPassword, string expectedMessage)
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);
            var request = new DashboardModel.ChangePasswordRequest
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            };

            // Act
            var result = await model.OnPostChangePasswordAsync(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var message = (string)jsonResult.Value!.GetType().GetProperty("message")!.GetValue(jsonResult.Value)!;
            Assert.Equal(expectedMessage, message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DashboardPageTest_Toggle2fa(bool enabled)
        {
            // Arrange
            await InitializeUser();
            var httpContext = _fixture.CreateAuthenticatedHttpContext(_userId, UserEmail);
            var model = CreateDashboardModel(httpContext);

            // Act
            var result = await model.OnPostToggle2faAsync(new DashboardModel.Toggle2faRequest { Enabled = enabled });

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value!.GetType().GetProperty("success")!.GetValue(jsonResult.Value)!;
            var resultEnabled = (bool)jsonResult.Value!.GetType().GetProperty("enabled")!.GetValue(jsonResult.Value)!;
            Assert.True(success);
            Assert.Equal(enabled, resultEnabled);
            var user = await _dbContext.Users.FindAsync(_userId);
            Assert.Equal(enabled, user!.TwoFactorEnabled);
        }
    }
}

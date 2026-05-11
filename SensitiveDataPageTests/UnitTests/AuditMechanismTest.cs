using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using SensitiveDataPage.Data;
using SensitiveDataPage.Services;

namespace SensitiveDataPageTests.UnitTests
{
    public class AuditMechanismTest
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly AuditMechanism _auditMechanism;

        public AuditMechanismTest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditMechanism = new AuditMechanism(_dbContext, _httpContextAccessorMock.Object);
        }

        [Fact]
        public void ValidData_AuditMechanismTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            
            //Act
            _auditMechanism.LogAudit(userId, "Action", "EntityType", "UserAgent", "TestDetails").ConfigureAwait(true);

            //Assert
            var log = _dbContext.AuditLogs.FirstOrDefault(a => a.UserId == userId);
            Assert.NotNull(log);
            Assert.Equal("Action", log.Action);
            Assert.Equal("EntityType", log.EntityType);
            Assert.Equal("UserAgent", log.UserAgent);
        }

        [Fact]
        public async Task InvalidData_AuditMechanismTest()
        {
            //Act & Assert
            await Assert.ThrowsAsync<MissingMethodException>(() => _auditMechanism.LogAudit(Guid.Empty, String.Empty, String.Empty, String.Empty, String.Empty));
        }
    }
}

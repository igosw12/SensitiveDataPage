using Microsoft.Extensions.Configuration;
using SensitiveDataPage.Services;
using Moq;


namespace SensitiveDataPageTests.UnitTests
{
    public class SendEmailTest
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly EmailSender _emailSender;

        public SendEmailTest()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["EmailStrings:EmailAddress"]).Returns("test@example.com");
            _configurationMock.Setup(c => c["EmailStrings:EmailPassword"]).Returns("password");

            _emailSender = new EmailSender(_configurationMock.Object);
        }

        [Fact]
        public async Task EmptyEmail_EmailSenderTests()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _emailSender.SendEmailAsync(null!, "Subject", "Message"));
        }

        [Fact]
        public async Task EmptySubject_EmailSenderTests()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _emailSender.SendEmailAsync("Email", null!, "Message"));
        }

        [Fact]
        public async Task EmptyMessage_EmailSenderTests()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _emailSender.SendEmailAsync("Email", "Subject", null!));
        }
    }
}

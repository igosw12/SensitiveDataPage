using Moq;
using SensitiveDataPage.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SensitiveDataPageTests.UnitTests
{
    public class EncryptTest
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly IEncrypt _encrypt;

        public EncryptTest()
        {
            _configurationMock = new Mock<IConfiguration>();
            var key = Convert.ToBase64String(new byte[32]);
            _configurationMock.Setup(c => c["Encryption:Key"]).Returns(key);
            _encrypt = new Encrypt(_configurationMock.Object);
        }

        [Fact]
        public void ValidData_EncryptTest()
        {
            //Arrange
            var password = "Password123";

            //Act
            var result = _encrypt.EncryptData(password);

            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.EncryptedData);
            Assert.NotEmpty(result.EncryptionIV);
            Assert.NotEmpty(result.EncryptionTag);
        }

        [Fact]
        public void NullKey_EncryptTest()
        {
            //Arrange
            var configurationMock = new Mock<IConfiguration>();

            //Act
            configurationMock.Setup(c => c["Encryption:Key"]).Returns((string?)null);

            //Assert 
            Assert.Throws<InvalidOperationException>(() => new Encrypt(configurationMock.Object));
        }
    }
}

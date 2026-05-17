using Microsoft.Extensions.Configuration;
using Moq;
using SensitiveDataPage.Services;

namespace SensitiveDataPageTests.ComponentTests
{
    public class EncryptDecryptTest
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly IEncrypt _encrypt;
        private readonly IDecrypt _decrypt;

        public EncryptDecryptTest()
        {
            _configurationMock = new Mock<IConfiguration>();
            var key = Convert.ToBase64String(new byte[32]);
            _configurationMock.Setup(c => c["Encryption:Key"]).Returns(key);
            _encrypt = new Encrypt(_configurationMock.Object);
            _decrypt = new Decrypt(_configurationMock.Object);
        }

        [Fact]
        public void EncryptDecrypt_ValidData_Test()
        {
            //Arrange
            var password = "Password123";

            //Act
            var encryptedData = _encrypt.EncryptData(password);
            var decryptedData = _decrypt.DecryptData(encryptedData.EncryptedData, encryptedData.EncryptionIV, encryptedData.EncryptionTag);
            
            //Assert
            Assert.NotEqual(encryptedData.ToString(), decryptedData);
            Assert.Equal(password, decryptedData);
        }

        [Fact]
        public void EncryptDecrypt_InvalidData_Test()
        {
            //Arrange
            var password = "Password123";
            var encryptedData = _encrypt.EncryptData(password);
            //Act & Assert
            Assert.Throws<ArgumentException>(() => _decrypt.DecryptData(encryptedData.EncryptedData, encryptedData.EncryptionIV, new byte[2]));
        }

        [Fact]
        public void EncryptDecrypt_NotEqual_Test()
        {
            //Arrange
            var password1 = "Password";
            var password2 = "Password";

            //Act
            var encryptedData1 = _encrypt.EncryptData(password1);
            var encryptedData2 = _encrypt.EncryptData(password2);
            var decryptedData1 = _decrypt.DecryptData(encryptedData1.EncryptedData, encryptedData1.EncryptionIV, encryptedData1.EncryptionTag);
            var decryptedData2 = _decrypt.DecryptData(encryptedData2.EncryptedData, encryptedData2.EncryptionIV, encryptedData2.EncryptionTag);

            //Arrange
            Assert.NotNull(decryptedData1);
            Assert.NotNull(decryptedData2);
            Assert.NotEqual(encryptedData1.EncryptedData, encryptedData2.EncryptedData);
            Assert.Equal(decryptedData1, decryptedData2);
        }
    }
}

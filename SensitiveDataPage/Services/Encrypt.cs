using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SensitiveDataPage.Services
{
    public class EncryptedResult
    {
        public byte[] EncryptedData { get; set; } = [];
        public byte[] EncryptionIV { get; set; } = [];
        public byte[] EncryptionTag { get; set; } = [];
    }

    public class Encrypt : IEncrypt
    {
        private readonly byte[] _key;

        public Encrypt(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption key is not configured.");
            _key = Convert.FromBase64String(keyBase64);
        }

        public EncryptedResult EncryptData(string plainText)
        {
            var iv = new byte[12];
            RandomNumberGenerator.Fill(iv);

            var tag = new byte[16];
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];

            using var aesGcm = new AesGcm(_key, 16);
            aesGcm.Encrypt(iv, plainBytes, cipherBytes, tag);

            return new EncryptedResult
            {
                EncryptedData = cipherBytes,
                EncryptionIV = iv,
                EncryptionTag = tag
            };
        }
    }
}

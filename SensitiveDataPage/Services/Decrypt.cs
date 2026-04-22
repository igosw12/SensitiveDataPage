using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SensitiveDataPage.Services
{
    public class Decrypt : IDecrypt
    {
        private readonly byte[] _key;

        public Decrypt(IConfiguration configuration)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption key is not configured.");
            _key = Convert.FromBase64String(keyBase64);
        }

        public string DecryptData(byte[] encryptedData, byte[] iv, byte[] tag)
        {
            var plainBytes = new byte[encryptedData.Length];

            using var aesGcm = new AesGcm(_key, 16);
            aesGcm.Decrypt(iv, encryptedData, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}

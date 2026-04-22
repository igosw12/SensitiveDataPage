namespace SensitiveDataPage.Services
{
    public interface IEncrypt
    {
        EncryptedResult EncryptData(string plainText);
    }
}

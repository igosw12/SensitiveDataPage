namespace SensitiveDataPage.Services
{
    public interface IDecrypt
    {
        string DecryptData(byte[] encryptedData, byte[] iv, byte[] tag);
    }
}

namespace SensitiveDataPage.Models
{
    public class SensitiveData
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public required byte[] EncryptedData { get; set; }
        public required byte[] EncryptionIV { get; set; }
        public required byte[] EncryptionTag { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User? User { get; set; }
    }
}

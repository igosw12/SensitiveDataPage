namespace SensitiveDataPage.Models
{
    public class TwoFactorToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? TokenHash { get; set; }
        public bool Used { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int DailyCount { get; set; }
        public DateTime DailyCountResetAt { get; set; }

        public User? User { get; set; }
    }
}

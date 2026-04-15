using System.ComponentModel.DataAnnotations;

namespace SensitiveDataPage.Models
{
    public class User
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string? Email { get; set; }
        [Required]
        [MaxLength(500)]
        public string? PasswordHash { get; set; }
        public DateTime DateOfBirth { get; set; }

        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<EmailVerificationToken>? EmailVerificationTokens { get; set; }
        public ICollection<PasswordResetToken>? PasswordResetTokens { get; set; }
        public ICollection<SensitiveData>? SensitiveDataItems { get; set; }
    }
}

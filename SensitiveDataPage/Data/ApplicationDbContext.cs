using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Models;

namespace SensitiveDataPage.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<SensitiveData> SensitiveData { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", "dbo");
                entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            });

            modelBuilder.Entity<EmailVerificationToken>(entity =>
            {
                entity.ToTable("EmailVerificationTokens", "dbo");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_EmailVerification_UserId");
                entity.HasOne(e => e.User).WithMany(u => u.EmailVerificationTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens", "dbo");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_PasswordReset_UserId");
                entity.HasOne(e => e.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SensitiveData>(entity =>
            {
                entity.ToTable("SensitiveData", "dbo");
                entity.HasIndex(s => s.UserId).HasDatabaseName("IX_SensitiveData_UserId");
                entity.HasOne(s => s.User).WithMany(u => u.SensitiveDataItems).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs", "dbo");
                entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

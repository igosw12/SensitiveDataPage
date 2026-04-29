using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using System.Security.Cryptography;
using System.Text;

namespace SensitiveDataPage.Pages
{
    public class EmailVerificationModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EmailVerificationModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public string? Token { get; set; }

        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var rawToken = Request.Query["token"].ToString();

            Token = rawToken;

            if (string.IsNullOrWhiteSpace(Token))
            {
                Success = false;
                Message = "Invalid token.";
                return Page();
            }
             
            string tokenHash;
            using (var sha = SHA256.Create())
            {
                var hashed = sha.ComputeHash(Encoding.UTF8.GetBytes(Token));
                tokenHash = Convert.ToBase64String(hashed);
            }

            var dbToken = await _db.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.Used && t.ExpiresAt > DateTime.UtcNow);

            if (dbToken == null)
            {
                Success = false;
                Message = "Token is invalid, expired, or already used.";
                return Page();
            }

            if (dbToken.User == null)
            {
                Success = false;
                Message = "Associated user not found.";
                return Page();
            }

            dbToken.Used = true;
            dbToken.User.IsVerified = true;
            dbToken.User.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            Success = true;
            Message = "Your email has been verified. You can now sign in.";

            return Page();
        }
    }
}

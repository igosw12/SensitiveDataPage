using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace SensitiveDataPage.Pages
{
    public class PasswordRestartModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PasswordRestartModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        public string? tokenHash { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
                ErrorMessage = "Password must have at least 8 characters, a capital letter and a number")]
            public required string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Password has to be the same")]
            public required string CheckPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var rawToken = Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(rawToken))
            {
                TempData["ErrorMessage"] = "Invalid link";
                return RedirectToPage("/Login");
            }

            tokenHash = await Hash(rawToken);

            var resetToken = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "Invalid link";
                return RedirectToPage("/Login");
            }

            if (resetToken.Used)
            {
                TempData["ErrorMessage"] = "Password Reset Link already used";
                return RedirectToPage("/Login");
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Link with password expired";
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            //TODO : find a way how to avoid using rawToken and hashing twice. 
            var rawToken = Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(rawToken))
            {
                TempData["ErrorMessage"] = "Unexpected issue happened. Please try again";
                return RedirectToPage("/Login");
            }

            tokenHash = await Hash(rawToken);

            var Token = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

            if (Token == null)
            {
                TempData["ErrorMessage"] = "Unexpected issue happened. Please try again";
                return RedirectToPage("/Login");
            }

            var passwordHash = await CreatePassword(Input.Password);
            await UpdatePassword(Token.UserId, Token.Id, passwordHash);

            TempData["SuccessMessage"] = "Password changed properly.";
            return RedirectToPage("/Login");
        }

        public async Task UpdatePassword(Guid userId, Guid tokenId, string passwordHash)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == tokenId);

            if (user == null || token == null)
            {
                TempData["ErrorMessage"] = "Unexpected issue happened. Please try again";
                return;
            }

            user.PasswordHash = passwordHash;
            user.UpdatedAt = DateTime.UtcNow;
            token.Used = true;

            await _db.SaveChangesAsync();
        }

        public Task<string> CreatePassword(string password)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            var passwordHash = Convert.ToBase64String(salt) + ":" + hash;
            return Task.FromResult(passwordHash);
        }

        public Task<string> Hash(string rawToken)
        {
            string tokenHash;
            var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            tokenHash = Convert.ToBase64String(hashed);
            return Task.FromResult(tokenHash);
        }
    }
}

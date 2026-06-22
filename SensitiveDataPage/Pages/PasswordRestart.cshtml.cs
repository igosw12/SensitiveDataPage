using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
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

        public string? TokenHash { get; set; }

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

            TokenHash = await Hash(rawToken);

            var resetToken = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.TokenHash == TokenHash);

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
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "error.invalidForm" });

            var rawToken = Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(rawToken))
                return new JsonResult(new { success = false, message = "error.unexpectedTryAgain" });

            TokenHash = await Hash(rawToken);

            var Token = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.TokenHash == TokenHash);

            if (Token == null)
                return new JsonResult(new { success = false, message = "error.unexpectedTryAgain" });

            var user = await _db.Users.FirstOrDefaultAsync(s => s.Id == Token.UserId);

            if (user == null || user.PasswordHash == null)
            {
                return new JsonResult(new { success = false, message = "error.unexpectedTryAgain" });
            }

            // Verify new password is not the same as old (using old salt for comparison only)
            var oldSalt = user.PasswordHash.Split(':')[0];
            var checkHash = await CreatePassword(Input.Password, oldSalt);
            if (user.PasswordHash == checkHash)
                return new JsonResult(new { success = false, message = "reset.newPasswordSameAsOld" });

            // Generate a fresh salt for the new password hash
            var newSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128 / 8));
            var passwordHash = await CreatePassword(Input.Password, newSalt);
            return await UpdatePassword(Token.UserId, Token.Id, passwordHash, user);
        }

        public async Task<IActionResult> UpdatePassword(Guid userId, Guid tokenId, string passwordHash, User user)
        {
            var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == tokenId);

            if (user == null || token == null)
                return new JsonResult(new { success = false, message = "error.unexpectedTryAgain" });

            user.PasswordHash = passwordHash;
            user.UpdatedAt = DateTime.UtcNow;
            token.Used = true;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "reset.success" });
        }

        public Task<string> CreatePassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            var passwordHash = Convert.ToBase64String(saltBytes) + ":" + hash;
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

using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

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
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (_db.EmailVerificationTokens.Any(t => t.TokenHash == Request.Query["token"]))
            {
                TempData["Error"] = "Invalid link";
                return RedirectToPage("/Login");
            }

            if (_db.PasswordResetTokens.All(t => t.TokenHash == Request.Query["token"] && t.Used))
            {
                TempData["Error"] = "Link with password reset was already used";
                return RedirectToPage("/Login");
            }
            var passwordHash = await CreatePassword(Input.Password);

            return RedirectToPage("/Login");
        }

        public Task<string> CreatePassword(string password)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Input.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            var passwordHash = Convert.ToBase64String(salt) + ":" + hash;

            return Task.FromResult(passwordHash);
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SensitiveDataPage.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public RegisterModel(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public string? RecaptchaToken { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
                ErrorMessage = "Password must have at least 8 characters, a capital letter and a number")]
            public required string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Password has to be the same")]
            public required string CheckPassword { get; set; }

            [Required]
            public DateTime DateOfBirth { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (RecaptchaToken == null)
            {
                return Page();
            }

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "Email already in use");
                return Page();
            }

            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Input.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            var passwordHash = Convert.ToBase64String(salt) + ":" + hash;

            var user = new User
            {
                Email = Input.Email,
                PasswordHash = passwordHash,
                DateOfBirth = Input.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(tokenBytes);
            var rawToken = WebEncoders.Base64UrlEncode(tokenBytes);

            string tokenHash;
            using (var sha = SHA256.Create())
            {
                var hashed = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
                tokenHash = Convert.ToBase64String(hashed);
            }

            _db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                User = user,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });

            await _db.SaveChangesAsync();

            var callbackUrl = Url.Page("/EmailVerification", null, new { token = rawToken }, Request.Scheme);
            var safeUrl = System.Net.WebUtility.HtmlEncode(callbackUrl);

            var emailBody = new StringBuilder();
            emailBody.AppendLine("Hello,");
            emailBody.AppendLine();
            emailBody.AppendLine("Click the link below to verify your email address:");
            emailBody.AppendLine($"<p><a href=\"{safeUrl}\">Verify your email address</a></p>");
            emailBody.AppendLine();
            emailBody.AppendLine("If you did not register, ignore this message.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("SensitiveData Admin team");

            await _emailSender.SendEmailAsync(Input.Email, "Account Verification", emailBody.ToString());

            TempData["InfoMessage"] = "Registration successful. Please check your email and verify your account before signing in.";

            return RedirectToPage("/Login");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}

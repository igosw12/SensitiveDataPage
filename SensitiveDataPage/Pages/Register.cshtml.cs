using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Services;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace SensitiveDataPage.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public RegisterModel (ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

[BindProperty]
        public required InputModel Input { get; set; }

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
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "error.invalidForm" });

            if (RecaptchaToken == null)
                return new JsonResult(new { success = false, message = "reg.recaptchaFail" });

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);
            if (existing != null)
                return new JsonResult(new { success = false, message = "reg.emailInUse" });

            var user = await CreateUserAsync().ConfigureAwait(false);
            var token = await CreateVerificationToken(user).ConfigureAwait(false);
            await SendEmailAsync(token).ConfigureAwait(false);
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "reg.registerSuccess" });
        }

        private Task<User> CreateUserAsync()
        {
            //Consider using a stronger hashing algorithm like Argon2 or bcrypt.
            //Consider move it to a separate service as it is reusable and it will make the code cleaner
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
            return Task.FromResult(user);
        }

        private Task<string> CreateVerificationToken(User user)
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(tokenBytes);
            string rawToken = WebEncoders.Base64UrlEncode(tokenBytes);

            string tokenHash;
            using (var sha = SHA256.Create())
            {
                var hashed = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
                tokenHash = Convert.ToBase64String(hashed);
            }

            var emailVerificationTokens = new EmailVerificationToken
            {
                User = user,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _db.EmailVerificationTokens.Add(emailVerificationTokens);
            return Task.FromResult(rawToken);
        }

        private async Task SendEmailAsync(string rawToken)
        {
            var callbackUrl = Url.Page("/EmailVerification", null, new { token = rawToken }, Request.Scheme);
            var safeUrl = System.Net.WebUtility.HtmlEncode(callbackUrl);

            var message = new MailMessage();
            message.To.Add(Input.Email);

            message.IsBodyHtml = true;

            message.Body = $@"
            <html>
            <body>
                <p>Hello,</p>
                <p></p>
                <p>Click below to verify your email address:</p>
                <p> </p>
                <a href='{callbackUrl}' 
                   style='display:inline-block;padding:12px 20px;
                          color:#fff;background:#007bff;
                          text-decoration:none;border-radius:5px;'>
                    Verify your email address
                </a>
                <p> </p>
                <p>If you didn't create an account on our page, please contact us.</p>

            </body>
            </html>";

            await _emailSender.SendEmailAsync(Input.Email, "Account Verification", message.Body);
        }
    }
}

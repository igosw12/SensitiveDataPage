using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using SensitiveDataPage.Services;

namespace SensitiveDataPage.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "error.invalidForm" });

            try
            {
                var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);

                if (existing is null)
                    return new JsonResult(new { success = false, message = "forgot.noAccount" });

                var rawToken = await PasswordResetToken(existing.Id);
                await SendEmailAsync(rawToken);
                return new JsonResult(new { success = true, message = "forgot.emailSent" });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "error.tryAgain" });
            }
        }

        private async Task<string> PasswordResetToken(Guid userId)
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(tokenBytes);
            string rawToken = WebEncoders.Base64UrlEncode(tokenBytes);

            string tokenHash;
            var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            tokenHash = Convert.ToBase64String(hashed);


            var resetTokenExist = _db.PasswordResetTokens.FirstOrDefault(u => u.UserId == userId);
            if (resetTokenExist != null)
            {
                if (resetTokenExist.Used == false)
                {
                    resetTokenExist.TokenHash = tokenHash;
                    resetTokenExist.ExpiresAt = DateTime.UtcNow.AddHours(24);
                    resetTokenExist.CreatedAt = DateTime.UtcNow;
                }
                else if (resetTokenExist.Used == true)
                {
                    resetTokenExist.TokenHash = tokenHash;
                    resetTokenExist.ExpiresAt = DateTime.UtcNow.AddHours(24);
                    resetTokenExist.CreatedAt = DateTime.UtcNow;
                    resetTokenExist.Used = false;
                }
            }
            else
            {
                var passwordResetTokens = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow
                };
                _db.PasswordResetTokens.Add(passwordResetTokens);
            }

            await _db.SaveChangesAsync();
            return rawToken;
        }

        private async Task SendEmailAsync(string rawToken)
        {
            var callbackUrl = Url.Page("/PasswordRestart", null, new { token = rawToken }, Request.Scheme);
            var safeUrl = System.Net.WebUtility.HtmlEncode(callbackUrl);

            var message = new MailMessage();
            message.To.Add(Input.Email);

            message.IsBodyHtml = true;

            message.Body = $@"
            <html>
            <body>
                <p>Hello,</p>
                <p></p>
                <p>Click below to restart your password:</p>
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

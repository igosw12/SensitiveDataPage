using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace SensitiveDataPage.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IAuditMechanism _auditMechanism;

        public LoginModel(ApplicationDbContext db, IEmailSender emailSender, IAuditMechanism auditMechanism)
        {
            _db = db;
            _emailSender = emailSender;
            _auditMechanism = auditMechanism;
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        [BindProperty]
        public TwoFactorInputModel? TwoFactorInput { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            public required string Password { get; set; }
        }

        public class TwoFactorInputModel
        {
            [Required]
            [StringLength(6, MinimumLength = 6)]
            public required string Code { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null)
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });

            if (user.IsActive is false && user.DeletedAt is not null)
                return new JsonResult(new { success = false, message = "login.accountDeleted" });

            if (user.Email == null)
                return new JsonResult(new { success = false, message = "login.emailMissing" });

            if (user.PasswordHash == null)
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });

            if (user.IsVerified is false)
                return new JsonResult(new { success = false, message = "login.notVerified" });

            var userPasswordHash = user.PasswordHash.Split(':');
            if (userPasswordHash.Length != 2)
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });

            var salt = Convert.FromBase64String(userPasswordHash[0]);
            var storedHash = userPasswordHash[1];

            var inputHash = await Decrypt(salt, Input.Password);

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(storedHash), Convert.FromBase64String(inputHash)))
            {
                if (user.FailedLoginAttempts < 5)
                {
                    user.FailedLoginAttempts += 1;
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();
                    await _auditMechanism.LogAudit(user.Id, "Trying to login with wrong password", "User", Request.Headers["User-Agent"].ToString(), "Wrong password");
                }
                else if (user.FailedLoginAttempts >= 5 && user.LockoutUntil < DateTime.UtcNow)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();
                    await _auditMechanism.LogAudit(user.Id, "5th failed Login Attempt - Locking account", "User", Request.Headers["User-Agent"].ToString(), "Account locked");
                    return new JsonResult(new { success = false, message = "login.locked" });
                }
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });
            }
            else if (user.LockoutUntil != null && user.LockoutUntil > DateTime.UtcNow)
            {
                await _auditMechanism.LogAudit(user.Id, "Trying to access locked account", "User", Request.Headers["User-Agent"].ToString(), "Trying to access locked account");
                return new JsonResult(new { success = false, message = "login.locked" });
            }

            if (user.TwoFactorEnabled == true)
            {
                var result = await TwoFactorManger(user);
                return result;
            }

            await SignInUser(user);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostVerifyTwoFactorAsync()
        {
            var userIdStr = TempData["TwoFactorUserId"]?.ToString();
            TempData.Keep("TwoFactorUserId");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return new JsonResult(new { success = false, message = "login.twoFactor.sessionExpired" });

            if (TwoFactorInput == null || string.IsNullOrWhiteSpace(TwoFactorInput.Code))
                return new JsonResult(new { success = false, message = "login.twoFactor.invalidCode" });

            var token = await _db.TwoFactorToken.FirstOrDefaultAsync(t =>
                t.UserId == userId && t.Used == false && t.ExpiresAt > DateTime.UtcNow);

            if (token == null)
                return new JsonResult(new { success = false, message = "login.twoFactor.expired" });

            if (token.TokenHash == null)
                return new JsonResult(new { success = false, message = "login.twoFactor.invalidCode" });

            var parts = token.TokenHash.Split(':');
            if (parts.Length != 2)
                return new JsonResult(new { success = false, message = "login.twoFactor.invalidCode" });

            var tokenSalt = Convert.FromBase64String(parts[0]);
            var storedTokenHash = parts[1];

            var inputHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: TwoFactorInput.Code.Trim(),
                salt: tokenSalt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(storedTokenHash),
                Convert.FromBase64String(inputHash)))
            {
                return new JsonResult(new { success = false, message = "login.twoFactor.invalidCode" });
            }

            token.Used = true;
            _db.TwoFactorToken.Update(token);
            await _db.SaveChangesAsync();

            TempData.Remove("TwoFactorUserId");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });

            await _auditMechanism.LogAudit(user.Id, "Successful Two Factor Authentication", "User", Request.Headers["User-Agent"].ToString(), "User passed two factor authentication successfully");
            await SignInUser(user);

            return new JsonResult(new { success = true });
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email!)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            var twoFactorTokenReset = _db.TwoFactorToken.FirstOrDefault(t => t.UserId == user.Id && t.TokenHash != null);

            if (twoFactorTokenReset != null && twoFactorTokenReset.DailyCount > 0)
            {
                twoFactorTokenReset.DailyCount = 0;
                twoFactorTokenReset.DailyCountResetAt = DateTime.UtcNow.AddDays(1);
                _db.TwoFactorToken.Update(twoFactorTokenReset);
                await _db.SaveChangesAsync();
            }

            await _auditMechanism.LogAudit(user.Id, "Successful Login", "User", Request.Headers["User-Agent"].ToString(), "User logged in successfully");
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        private async Task<IActionResult> TwoFactorManger(User user)
        {
            var existingToken = await _db.TwoFactorToken.FirstOrDefaultAsync(u => u.UserId == user.Id && u.TokenHash != null);

            if (existingToken != null && existingToken.DailyCount >= 5 && existingToken.DailyCountResetAt > DateTime.UtcNow)
            {
                await _auditMechanism.LogAudit(user.Id, "Failed Two Factor Authentication", "User", Request.Headers["User-Agent"].ToString(), "User failed two factor authentication");
                return new JsonResult(new { success = false, message = "login.tooManyAttempts" });
            }

            if (existingToken == null)
            {
                int code = RandomNumberGenerator.GetInt32(100000, 999999);
                var encryptedCode = await Encrypt(code.ToString());

                var twoFactorToken = new TwoFactorToken
                {
                    UserId = user.Id,
                    TokenHash = encryptedCode,
                    Used = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    DailyCount = 1,
                    DailyCountResetAt = DateTime.UtcNow.AddDays(1)
                };

                _db.TwoFactorToken.Add(twoFactorToken);
                await _db.SaveChangesAsync();
                await _emailSender.SendEmailAsync(user.Email!, "Two Factor Authentication", $"Your code is: {code} ");
            }
            else
            {
                int code = RandomNumberGenerator.GetInt32(100000, 999999);
                var encryptedCode = await Encrypt(code.ToString());

                existingToken.TokenHash = encryptedCode;
                existingToken.Used = false;
                existingToken.CreatedAt = DateTime.UtcNow;
                existingToken.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                existingToken.DailyCount += 1;
                existingToken.DailyCountResetAt = DateTime.UtcNow.AddDays(1);

                _db.TwoFactorToken.Update(existingToken);
                await _db.SaveChangesAsync();
                await _emailSender.SendEmailAsync(user.Email!, "Two Factor Authentication", $"Your code is: {code} ");
            }

            TempData["TwoFactorUserId"] = user.Id.ToString();

            return new JsonResult(new { success = false, twoFactorRequired = true, message = "login.twoFactor.codeSent" });
        }

        private async Task<string> Encrypt(string code)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: code.ToString(),
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            var tokenHash = Convert.ToBase64String(salt) + ":" + hash;

            return tokenHash;
        }

        private async Task<string> Decrypt(byte[] salt, string password)
        {
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            return hash;
        }
    }
}

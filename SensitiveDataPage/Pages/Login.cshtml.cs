using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
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
        
        public LoginModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            public required string Password { get; set; }
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

            var hash = await Decrypt(salt, storedHash);

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(storedHash), Convert.FromBase64String(hash)))
                return new JsonResult(new { success = false, message = "login.wrongCredentials" });

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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        private async Task<string> Decrypt(byte[] salt, string storedHash)
        {
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Input.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            return hash;
        }
    }
}

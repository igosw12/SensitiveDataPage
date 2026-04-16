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
            if (!ModelState.IsValid) return Page();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Wrong Email or Password. Please try again.";
                return Page();
            }

            if (user.PasswordHash == null)
            {
                TempData["ErrorMessage"] = "Wrong Email or Password. Please try again.";
                return Page();
            }

            var parts = user.PasswordHash.Split(':');
            if (parts.Length != 2)
            {
                TempData["ErrorMessage"] = "Wrong Email or Password. Please try again.";
                return Page();
            }

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Input.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(storedHash), Convert.FromBase64String(hash)))
            {
                TempData["ErrorMessage"] = "Wrong Email or Password. Please try again.";
                return Page();
            }

            if (user.IsVerified is false)
            {
                TempData["ErrorMessage"] = "Account is not verified yet. Activation link is on provided email. ";
                return Page();
            }

            await SignInUser(user);

            return RedirectToPage("/Dashboard");
        }

        private async Task SignInUser(User user)
        {

            if (user.Email == null)
            {
                TempData["ErrorMessage"] = "User email is missing. Please contact support.";
                return;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}

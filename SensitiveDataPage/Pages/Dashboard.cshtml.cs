using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SensitiveDataPage.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        public string Email { get; set; }

        public void OnGet()
        {
            Email = User.FindFirstValue(ClaimTypes.Email);
        }

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}

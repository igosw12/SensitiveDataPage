using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;
using SensitiveDataPage.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace SensitiveDataPage.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IEncrypt _encrypt;
        private readonly IDecrypt _decrypt;

        public DashboardModel(ApplicationDbContext db, IEncrypt encrypt, IDecrypt decrypt)
        {
            _db = db;
            _encrypt = encrypt;
            _decrypt = decrypt;
        }

        public required string Email { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public class SaveDataRequest
        {
            public int Tier { get; set; }
            public string Type { get; set; } = "";
            public string Category { get; set; } = "";
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        public class DataRecord
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = "";
            public string Category { get; set; } = "";
            public Dictionary<string, string> Fields { get; set; } = new();
            public DateTime CreatedAt { get; set; }
        }

        public class UpdateDataRequest
        {
            public Guid Id { get; set; }
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        public class DeleteDataRequest
        {
            public Guid Id { get; set; }
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
        }

        public class Toggle2faRequest
        {
            public bool Enabled { get; set; }
        }

        public async Task OnGetAsync()
        {
            Email = User.FindFirstValue(ClaimTypes.Email)!;
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            TwoFactorEnabled = user?.TwoFactorEnabled ?? false;
        }

        public IActionResult OnGetTier1Data() => TierInfo(1, "Confidential data");
        public IActionResult OnGetTier2Data() => TierInfo(2, "Sensitive data");
        public IActionResult OnGetTier3Data() => TierInfo(3, "Public data");
        public IActionResult OnGetTier4Data() => TierInfo(4, "Custom data");

        private IActionResult TierInfo(int tier, string message) =>
            new JsonResult(new { success = true, tier, message });

        public async Task<IActionResult> OnGetEntriesAsync(int tier)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var records = await _db.SensitiveData
                .Where(s => s.UserId == userId && s.DeletedAt == null)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var result = new List<DataRecord>();
            foreach (var record in records)
            {
                try
                {
                    var json = _decrypt.DecryptData(record.EncryptedData, record.EncryptionIV, record.EncryptionTag);
                    var entry = JsonSerializer.Deserialize<SaveDataRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (entry != null && entry.Tier == tier)
                        result.Add(new DataRecord
                        {
                            Id = record.Id,
                            Type = entry.Type,
                            Category = entry.Category,
                            Fields = entry.Fields,
                            CreatedAt = record.CreatedAt
                        });
                }
                catch { }
            }

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostDeleteAccountAsync()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _db.Users.FirstOrDefaultAsync(s => s.Id == userId && s.DeletedAt == null);

            if (user is null)
            {
                return new JsonResult(new { success = false });
            }

            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;

            await _db.SensitiveData
                .Where(s => s.UserId == userId && s.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeletedAt, DateTime.UtcNow));

            await _db.SaveChangesAsync();
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostSaveDataAsync([FromBody] SaveDataRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Category)
                || request.Fields == null || !request.Fields.Any(f => !string.IsNullOrWhiteSpace(f.Value)))
                return new JsonResult(new { success = false, message = "Name, category and at least one field are required." });

            var json = JsonSerializer.Serialize(request);
            var encrypted = _encrypt.EncryptData(json);

            _db.SensitiveData.Add(new SensitiveData
            {
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                EncryptedData = encrypted.EncryptedData,
                EncryptionIV = encrypted.EncryptionIV,
                EncryptionTag = encrypted.EncryptionTag,
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            });
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateDataAsync([FromBody] UpdateDataRequest request)
        {
            if (request.Fields == null || !request.Fields.Any(f => !string.IsNullOrWhiteSpace(f.Value)))
                return new JsonResult(new { success = false, message = "At least one field is required." });

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var record = await _db.SensitiveData.FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId && s.DeletedAt == null);
            if (record == null)
                return new JsonResult(new { success = false, message = "Record not found." });

            var json = _decrypt.DecryptData(record.EncryptedData, record.EncryptionIV, record.EncryptionTag);
            var existing = JsonSerializer.Deserialize<SaveDataRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            existing.Fields = request.Fields;

            var encrypted = _encrypt.EncryptData(JsonSerializer.Serialize(existing));
            record.EncryptedData = encrypted.EncryptedData;
            record.EncryptionIV = encrypted.EncryptionIV;
            record.EncryptionTag = encrypted.EncryptionTag;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteDataAsync([FromBody] DeleteDataRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var record = await _db.SensitiveData.FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId);
            if (record == null)
                return new JsonResult(new { success = false, message = "Record not found." });

            record.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostChangePasswordAsync([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return new JsonResult(new { success = false, message = "All fields are required." });

            if (request.NewPassword != request.ConfirmPassword)
                return new JsonResult(new { success = false, message = "New passwords do not match." });

            if (request.NewPassword == request.OldPassword)
                return new JsonResult(new { success = false, message = "Current and new passwords can't be the same" });

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
                return new JsonResult(new { success = false, message = "User not found." });

            var parts = user.PasswordHash!.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];
            var inputHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.OldPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(storedHash), Convert.FromBase64String(inputHash)))
                return new JsonResult(new { success = false, message = "Current password is incorrect." });

            var newSalt = RandomNumberGenerator.GetBytes(128 / 8);
            var newHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.NewPassword,
                salt: newSalt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            user.PasswordHash = $"{Convert.ToBase64String(newSalt)}:{newHash}";
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostToggle2faAsync([FromBody] Toggle2faRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
                return new JsonResult(new { success = false, message = "User not found." });

            user.TwoFactorEnabled = request.Enabled;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true, enabled = user.TwoFactorEnabled });
        }
    }
}

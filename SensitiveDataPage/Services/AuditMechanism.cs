using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using SensitiveDataPage.Models;

namespace SensitiveDataPage.Services
{
    public class AuditMechanism : IAuditMechanism
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditMechanism(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAudit(Guid userId, string action, string entityType, string userAgent, string details)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var auditLog = new AuditLog
            {
                Id = new Guid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                IpAddress = ip,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            if (auditLog != null)
            {
                await _db.AuditLogs.AddAsync(auditLog);
            } 
            else
            {
                throw new Exception("Failed to create audit log entry.");
            }

            await _db.SaveChangesAsync();
        }
    }
}
namespace SensitiveDataPage.Services
{
    public interface IAuditMechanism
    {
        Task LogAudit(Guid userId, string action, string entityType, string userAgent, string details);
    }
}

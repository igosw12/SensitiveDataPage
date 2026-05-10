using Microsoft.EntityFrameworkCore;
using SensitiveDataPage.Data;
using System.Data;

namespace SensitiveDataPage.Services
{
    public class DailyCountResetMechanism : BackgroundService
    {
        private readonly ILogger<DailyCountResetMechanism> _logger;
        private readonly IServiceScopeFactory _scopeFactory;


        public DailyCountResetMechanism(ILogger<DailyCountResetMechanism> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting DailyCountResetMechanism.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ResetDailyCountsIfNeeded(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while resetting daily counts.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        private async Task ResetDailyCountsIfNeeded(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currentDate = DateTime.UtcNow;

            var twoFactorTokens = await db.TwoFactorToken.Where(t => t.DailyCountResetAt <= currentDate).ToListAsync();

            foreach (var token in twoFactorTokens)
            {
                token.DailyCount = 0;
                token.DailyCountResetAt = currentDate.AddDays(1);
            }

            var userBlockade = await db.Users.Where(u => u.LockoutUntil <= currentDate).ToListAsync();

            foreach (var user in userBlockade)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutUntil = null;
            }

            if (twoFactorTokens.Count > 0 || userBlockade.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
        }
    }
}

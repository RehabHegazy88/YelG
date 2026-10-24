using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Infrastructure.Email
{
    /// <summary>
    /// Sends the notification emails outside the request: the visitor's answer never waits for the mail
    /// server. Unsent notifications (server down, settings missing) are retried every few minutes, up to
    /// <see cref="EnquiryOptions.MaxNotificationAttempts"/>; the enquiry itself is already saved.
    /// </summary>
    public sealed class EnquiryNotificationService : BackgroundService, IEnquiryNotificationQueue
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

        private readonly Channel<long> _queue = Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = true });
        private readonly IServiceScopeFactory _scopes;
        private readonly EnquiryOptions _options;
        private readonly TimeProvider _clock;
        private readonly ILogger<EnquiryNotificationService> _logger;

        public EnquiryNotificationService(IServiceScopeFactory scopes, IOptions<EnquiryOptions> options, TimeProvider clock,
            ILogger<EnquiryNotificationService> logger)
        {
            _scopes = scopes;
            _options = options.Value;
            _clock = clock;
            _logger = logger;
        }

        public void Enqueue(long enquiryId) => _queue.Writer.TryWrite(enquiryId);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var nextSweep = DateTimeOffset.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_clock.GetUtcNow() >= nextSweep)
                    {
                        nextSweep = _clock.GetUtcNow() + SweepInterval;
                        foreach (var id in await PendingAsync(stoppingToken)) await SendAsync(id, stoppingToken);
                    }

                    using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    wait.CancelAfter(SweepInterval);
                    try
                    {
                        var id = await _queue.Reader.ReadAsync(wait.Token);
                        await SendAsync(id, stoppingToken);
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // time for the next sweep
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Enquiry notification loop failed; retrying");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }
        }

        /// <summary>Unsent notifications of the last week that may be tried again (waiting longer after each failure).</summary>
        private async Task<List<long>> PendingAsync(CancellationToken ct)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var now = _clock.GetUtcNow().UtcDateTime;
            var candidates = await db.Enquiries.AsNoTracking()
                .Where(e => e.NotificationSentAt == null && e.NotificationAttempts > 0 && e.NotificationAttempts < _options.MaxNotificationAttempts
                            && e.AddedDate > now.AddDays(-7))
                .Select(e => new { e.Id, e.NotificationAttempts, e.LastModifiedDate })
                .ToListAsync(ct);
            return candidates.Where(c => c.LastModifiedDate.AddMinutes(5 * c.NotificationAttempts) <= now).Select(c => c.Id).ToList();
        }

        /// <summary>Sends one notification and records the outcome on the enquiry.</summary>
        public async Task SendAsync(long id, CancellationToken ct)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var enquiry = await db.Enquiries.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (enquiry is null || enquiry.NotificationSentAt is not null) return;

            try
            {
                var site = await db.SiteSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(ct) ?? new SiteSettings();
                await scope.ServiceProvider.GetRequiredService<IEnquiryMailer>().SendAsync(enquiry, site, ct);
                enquiry.NotificationSentAt = _clock.GetUtcNow().UtcDateTime;
                enquiry.NotificationError = null;
                _logger.LogInformation("Notification sent for enquiry {EnquiryId}", id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                enquiry.NotificationError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                _logger.LogError(ex, "Notification email for enquiry {EnquiryId} failed (attempt {Attempt})", id, enquiry.NotificationAttempts + 1);
            }
            enquiry.NotificationAttempts++;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}

using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Careers;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Infrastructure.Email
{
    /// <summary>
    /// Sends the notification emails for new enquiries and job applications outside the request: the visitor's
    /// answer never waits for the mail server. Unsent notifications (server down, settings missing) are retried
    /// every few minutes, up to <see cref="EnquiryOptions.MaxNotificationAttempts"/>; the record itself is
    /// already saved.
    /// </summary>
    public sealed class NotificationService : BackgroundService, IEnquiryNotificationQueue, IApplicationNotificationQueue
    {
        private enum Kind { Enquiry, Application }

        private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

        private readonly Channel<(Kind Kind, long Id)> _queue = Channel.CreateUnbounded<(Kind, long)>(new UnboundedChannelOptions { SingleReader = true });
        private readonly IServiceScopeFactory _scopes;
        private readonly EnquiryOptions _options;
        private readonly TimeProvider _clock;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IServiceScopeFactory scopes, IOptions<EnquiryOptions> options, TimeProvider clock,
            ILogger<NotificationService> logger)
        {
            _scopes = scopes;
            _options = options.Value;
            _clock = clock;
            _logger = logger;
        }

        void IEnquiryNotificationQueue.Enqueue(long enquiryId) => _queue.Writer.TryWrite((Kind.Enquiry, enquiryId));

        void IApplicationNotificationQueue.Enqueue(long applicationId) => _queue.Writer.TryWrite((Kind.Application, applicationId));

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
                        foreach (var item in await PendingAsync(stoppingToken)) await SendAsync(item, stoppingToken);
                    }

                    using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    wait.CancelAfter(SweepInterval);
                    try
                    {
                        await SendAsync(await _queue.Reader.ReadAsync(wait.Token), stoppingToken);
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
                    _logger.LogError(ex, "Notification loop failed; retrying");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }
        }

        /// <summary>Unsent notifications of the last week that may be tried again (waiting longer after each failure).</summary>
        private async Task<List<(Kind, long)>> PendingAsync(CancellationToken ct)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var now = _clock.GetUtcNow().UtcDateTime;
            var max = _options.MaxNotificationAttempts;
            var enquiries = await db.Enquiries.AsNoTracking()
                .Where(e => e.NotificationSentAt == null && e.NotificationAttempts > 0 && e.NotificationAttempts < max && e.AddedDate > now.AddDays(-7))
                .Select(e => new { e.Id, e.NotificationAttempts, e.LastModifiedDate }).ToListAsync(ct);
            var applications = await db.JobApplications.AsNoTracking()
                .Where(a => a.NotificationSentAt == null && a.NotificationAttempts > 0 && a.NotificationAttempts < max && a.AddedDate > now.AddDays(-7))
                .Select(a => new { a.Id, a.NotificationAttempts, a.LastModifiedDate }).ToListAsync(ct);
            return enquiries.Where(c => c.LastModifiedDate.AddMinutes(5 * c.NotificationAttempts) <= now).Select(c => (Kind.Enquiry, c.Id))
                .Concat(applications.Where(c => c.LastModifiedDate.AddMinutes(5 * c.NotificationAttempts) <= now).Select(c => (Kind.Application, c.Id)))
                .ToList();
        }

        public Task SendEnquiryAsync(long id, CancellationToken ct) => SendAsync((Kind.Enquiry, id), ct);

        public Task SendApplicationAsync(long id, CancellationToken ct) => SendAsync((Kind.Application, id), ct);

        /// <summary>Sends one notification and records the outcome on its record.</summary>
        private async Task SendAsync((Kind Kind, long Id) item, CancellationToken ct)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var mailer = scope.ServiceProvider.GetRequiredService<IEnquiryMailer>();

            if (item.Kind == Kind.Enquiry)
            {
                var enquiry = await db.Enquiries.FirstOrDefaultAsync(e => e.Id == item.Id, ct);
                if (enquiry is null || enquiry.NotificationSentAt is not null) return;
                var (sentAt, error) = await TryAsync(async () => await mailer.SendAsync(enquiry, await SiteAsync(db, ct), ct), "enquiry", item.Id, enquiry.NotificationAttempts, ct);
                enquiry.NotificationSentAt = sentAt;
                enquiry.NotificationError = error;
                enquiry.NotificationAttempts++;
            }
            else
            {
                var application = await db.JobApplications.Include(a => a.Answers).FirstOrDefaultAsync(a => a.Id == item.Id, ct);
                if (application is null || application.NotificationSentAt is not null) return;
                var (sentAt, error) = await TryAsync(async () => await mailer.SendApplicationAsync(application, await SiteAsync(db, ct), ct), "application", item.Id, application.NotificationAttempts, ct);
                application.NotificationSentAt = sentAt;
                application.NotificationError = error;
                application.NotificationAttempts++;
            }
            await db.SaveChangesAsync(CancellationToken.None);
        }

        private static async Task<SiteSettings> SiteAsync(WebSiteDbContext db, CancellationToken ct) =>
            await db.SiteSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(ct) ?? new SiteSettings();

        private async Task<(DateTime? SentAt, string? Error)> TryAsync(Func<Task> send, string what, long id, int attempts, CancellationToken ct)
        {
            try
            {
                await send();
                _logger.LogInformation("Notification sent for {What} {Id}", what, id);
                return (_clock.GetUtcNow().UtcDateTime, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Notification email for {What} {Id} failed (attempt {Attempt})", what, id, attempts + 1);
                return (null, ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message);
            }
        }
    }
}

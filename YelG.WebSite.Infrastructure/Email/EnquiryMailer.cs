using System.Globalization;
using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Settings;

namespace YelG.WebSite.Infrastructure.Email
{
    /// <summary>
    /// Sends the notifications for new enquiries (to the notification address) and job applications (to the careers
    /// address), with the visitor as Reply-To. CVs are not attached: the email links to the admin panel.
    /// </summary>
    public interface IEnquiryMailer
    {
        Task SendAsync(Enquiry enquiry, SiteSettings site, CancellationToken ct);

        Task SendApplicationAsync(JobApplication application, SiteSettings site, CancellationToken ct);
    }

    public sealed class EnquiryMailer : IEnquiryMailer
    {
        private readonly SmtpOptions _smtp;
        private readonly string? _baseUrl;

        public EnquiryMailer(IOptions<SmtpOptions> smtp, IConfiguration configuration)
        {
            _smtp = smtp.Value;
            _baseUrl = configuration["Site:BaseUrl"]?.TrimEnd('/');
        }

        public async Task SendAsync(Enquiry enquiry, SiteSettings site, CancellationToken ct)
        {
            var to = FirstAddress(site.NotificationEmail, site.ContactEmail);
            using var message = Compose(enquiry, to);
            await DeliverAsync(message, ct);
        }

        public async Task SendApplicationAsync(JobApplication application, SiteSettings site, CancellationToken ct)
        {
            var to = FirstAddress(site.CareersEmail, site.NotificationEmail, site.ContactEmail);
            using var message = ComposeApplication(application, to);
            await DeliverAsync(message, ct);
        }

        private static string FirstAddress(params string[] addresses) =>
            addresses.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a))
            ?? throw new InvalidOperationException("No notification address in the site settings.");

        private async Task DeliverAsync(MimeMessage message, CancellationToken ct)
        {
            if (!_smtp.IsConfigured) throw new InvalidOperationException("SMTP is not configured (Smtp:Host and Smtp:FromAddress).");
            using var client = new SmtpClient { Timeout = _smtp.TimeoutSeconds * 1000 };
            var security = _smtp.Security.ToLowerInvariant() switch
            {
                "sslonconnect" => SecureSocketOptions.SslOnConnect,
                "none" => SecureSocketOptions.None,
                _ => SecureSocketOptions.StartTls
            };
            await client.ConnectAsync(_smtp.Host, _smtp.Port, security, ct);
            if (!string.IsNullOrEmpty(_smtp.UserName)) await client.AuthenticateAsync(_smtp.UserName, _smtp.Password ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);
        }

        /// <summary>The email: bilingual labels (Arabic first), plain text and HTML. Visitor text is encoded in the HTML part.</summary>
        public MimeMessage Compose(Enquiry e, string to)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.ReplyTo.Add(new MailboxAddress(e.Name, e.Email));
            message.Subject = OneLine($"طلب استشارة جديد / New enquiry: {e.Name} — {e.Company}");

            var link = _baseUrl is null ? null : $"{_baseUrl}/admin/enquiries/{e.Id}";
            var rows = new List<(string Label, string Value)>
            {
                ("الاسم / Name", e.Name),
                ("البريد / Email", e.Email),
                ("الشركة / Company", e.Company),
                ("الهاتف / Phone", e.Phone ?? "—"),
                ("الخدمة / Service", e.ServiceLabel),
                ("لغة الصفحة / Page language", e.Culture == "en" ? "English" : "العربية"),
                ("الموافقة / Consent", e.ConsentAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))
            };
            if (e.SelfCheckScore is { } score) rows.Add(("نتيجة الفحص المالي / Self-check score", score.ToString(CultureInfo.InvariantCulture)));

            message.Body = Body("طلب استشارة جديد · New enquiry", rows, e.Message, link);
            return message;
        }

        public MimeMessage ComposeApplication(JobApplication a, string to)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.ReplyTo.Add(new MailboxAddress(a.Name, a.Email));
            message.Subject = OneLine($"طلب توظيف جديد / New application: {a.RoleTitle} — {a.Name}");

            var link = _baseUrl is null ? null : $"{_baseUrl}/admin/applications/{a.Id}";
            var rows = new List<(string Label, string Value)>
            {
                ("الوظيفة / Role", a.RoleTitle),
                ("الاسم / Name", a.Name),
                ("البريد / Email", a.Email),
                ("الهاتف / Phone", a.Phone ?? "—"),
                ("LinkedIn", a.LinkedInUrl ?? "—"),
                ("السيرة الذاتية / CV", $"{a.CvFileName} ({Math.Max(1, a.CvSizeBytes / 1024)} KB)"),
                ("لغة الصفحة / Page language", a.Culture == "en" ? "English" : "العربية"),
                ("الموافقة / Consent", a.ConsentAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))
            };
            message.Body = Body("طلب توظيف جديد · New job application", rows, a.Message ?? "—", link);
            return message;
        }

        /// <summary>Bilingual labels (Arabic first), plain text and HTML; the visitor's text is encoded in the HTML part.</summary>
        private static MimeEntity Body(string heading, List<(string Label, string Value)> rows, string messageText, string? link)
        {
            var text = new StringBuilder();
            foreach (var (label, value) in rows) text.Append(label).Append(": ").AppendLine(value);
            text.AppendLine().AppendLine("الرسالة / Message:").AppendLine(messageText);
            if (link is not null) text.AppendLine().AppendLine(link);

            var html = new StringBuilder("<div dir=\"rtl\" style=\"font-family:Arial,sans-serif;font-size:14px;color:#1F2A4D\">");
            html.Append("<h2 style=\"color:#0A3177;font-size:18px\">").Append(Enc(heading)).Append("</h2><table cellpadding=\"6\" style=\"border-collapse:collapse\">");
            foreach (var (label, value) in rows)
                html.Append("<tr><td style=\"color:#5B6890;white-space:nowrap\">").Append(Enc(label)).Append("</td><td dir=\"auto\"><b>").Append(Enc(value)).Append("</b></td></tr>");
            html.Append("</table><p style=\"color:#5B6890;margin:16px 0 4px\">الرسالة / Message</p><div dir=\"auto\" style=\"white-space:pre-wrap;border-inline-start:3px solid #82D0DC;padding:8px 12px;background:#F4F7FD\">")
                .Append(Enc(messageText)).Append("</div>");
            if (link is not null) html.Append("<p><a href=\"").Append(Enc(link)).Append("\">فتح في لوحة التحكم · Open in the admin panel</a></p>");
            html.Append("</div>");
            return new BodyBuilder { TextBody = text.ToString(), HtmlBody = html.ToString() }.ToMessageBody();
        }

        private static string Enc(string value) => WebUtility.HtmlEncode(value);

        private static string OneLine(string value) => value.Replace('\r', ' ').Replace('\n', ' ');
    }
}

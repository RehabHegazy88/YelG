using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Application.Enquiries
{
    /// <summary>"Enquiries" configuration.</summary>
    public sealed class EnquiryOptions
    {
        public const string SectionName = "Enquiries";

        /// <summary>Secret mixed into the IP hash, so stored hashes cannot be reversed by trying every address. Required in production.</summary>
        public string IpHashSalt { get; set; } = string.Empty;

        /// <summary>How many times the notification email is tried before it is left for an editor to resend.</summary>
        public int MaxNotificationAttempts { get; set; } = 5;
    }

    /// <summary>Hands a saved enquiry to the email sender, which runs after the visitor has their answer.</summary>
    public interface IEnquiryNotificationQueue
    {
        void Enqueue(long enquiryId);
    }

    public enum EnquiryOutcome
    {
        Saved,
        Invalid,

        /// <summary>The honeypot was filled: the visitor (a bot) is told it worked, nothing is kept.</summary>
        Ignored
    }

    public sealed record EnquiryResult(EnquiryOutcome Outcome, IReadOnlyList<string> Errors, long? EnquiryId = null);

    /// <summary>Checks and saves a consultation request, then queues its notification email.</summary>
    public sealed partial class EnquiryService
    {
        private readonly IContentStore _store;
        private readonly IEnquiryNotificationQueue _queue;
        private readonly EnquiryOptions _options;
        private readonly TimeProvider _clock;

        public EnquiryService(IContentStore store, IEnquiryNotificationQueue queue, IOptions<EnquiryOptions> options, TimeProvider clock)
        {
            _store = store;
            _queue = queue;
            _options = options.Value;
            _clock = clock;
        }

        /// <summary>The services a visitor can choose (published, in a published category), by key.</summary>
        public async Task<IReadOnlyDictionary<string, Service>> ChoosableServicesAsync(CancellationToken ct = default)
        {
            var categories = (await _store.ListAsync<ServiceCategory>(c => c.IsPublished, ct)).Select(c => c.Id).ToHashSet();
            return (await _store.ListAsync<Service>(s => s.IsPublished, ct))
                .Where(s => categories.Contains(s.CategoryId))
                .ToDictionary(s => s.Key, StringComparer.Ordinal);
        }

        public async Task<EnquiryResult> SubmitAsync(EnquiryInput input, EnquiryOrigin origin, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(input.Website)) return new EnquiryResult(EnquiryOutcome.Ignored, []);

            var services = await ChoosableServicesAsync(ct);
            var errors = EnquiryRules.Validate(input, services.Keys.ToHashSet());
            if (errors.Count > 0) return new EnquiryResult(EnquiryOutcome.Invalid, errors);

            var culture = origin.Culture == "en" ? "en" : "ar";
            var consult = await _store.FirstAsync<ConsultSection>(ct);
            var serviceKey = EnquiryRules.Trim(input.Service);
            var service = services.GetValueOrDefault(serviceKey);
            var now = _clock.GetUtcNow().UtcDateTime;
            var phone = EnquiryRules.Trim(input.Phone);

            var enquiry = new Enquiry
            {
                Culture = culture,
                Name = EnquiryRules.Trim(input.Name),
                Email = EnquiryRules.Trim(input.Email),
                Company = EnquiryRules.Trim(input.Company),
                Phone = phone.Length == 0 ? null : phone,
                ServiceId = service?.Id,
                IsNotSure = service is null,
                ServiceLabel = Cut(service?.Name.For(culture) ?? consult?.NotSureLabel.For(culture) ?? EnquiryInput.NotSure, 200),
                Message = EnquiryRules.Trim(input.Message).Replace("\r\n", "\n"),
                ConsentAt = now,
                ConsentText = Cut(Plain(consult?.ConsentText.For(culture)), 1000),
                SelfCheckScore = input.SelfCheckScore is >= 0 and <= 10000 ? input.SelfCheckScore : null,
                IpHash = HashIp(origin.IpAddress),
                UserAgent = origin.UserAgent is null ? null : Cut(origin.UserAgent, 500)
            };
            _store.Add(enquiry);
            await _store.SaveChangesAsync(ct);

            _queue.Enqueue(enquiry.Id);
            return new EnquiryResult(EnquiryOutcome.Saved, [], enquiry.Id);
        }

        /// <summary>SHA-256 of the salted address, hex; null without an address.</summary>
        public string? HashIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_options.IpHashSalt + "|" + ip.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Cut(string value, int max) => value.Length <= max ? value : value[..max];

        /// <summary>The consent wording as the visitor read it: the site markup removed.</summary>
        private static string Plain(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var plain = LinkMarkup().Replace(text, "$1");
            plain = plain.Replace("\\*", "\u0000").Replace("*", "").Replace("\u0000", "*");
            return Regex.Replace(plain, @"\s+", " ").Trim();
        }

        [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
        private static partial Regex LinkMarkup();
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Application.Careers
{
    /// <summary>"Careers" configuration: where CVs are kept and how large they may be.</summary>
    public sealed class CareersOptions
    {
        public const string SectionName = "Careers";

        /// <summary>Private folder for CVs (never served as static files). Relative paths are under the content root.</summary>
        public string CvStoragePath { get; set; } = Path.Combine("App_Data", "applications");

        public long MaxCvBytes { get; set; } = 5 * 1024 * 1024;
    }

    /// <summary>The CV formats accepted, recognised by their first bytes: PDF, Word (.docx) and old Word (.doc).</summary>
    public sealed record CvFileType(string ContentType, string Extension)
    {
        public static readonly CvFileType Pdf = new("application/pdf", ".pdf");
        public static readonly CvFileType Docx = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx");
        public static readonly CvFileType Doc = new("application/msword", ".doc");

        public const string Accept = ".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        /// <param name="head">The first bytes of the file (at least 8).</param>
        /// <param name="fileName">Tells a .docx from other zip files, which share the same first bytes.</param>
        public static CvFileType? Detect(ReadOnlySpan<byte> head, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (head.Length >= 5 && head[..5].SequenceEqual("%PDF-"u8)) return Pdf;
            if (head.Length >= 4 && head[0] == 0x50 && head[1] == 0x4B && head[2] == 0x03 && head[3] == 0x04 && ext == ".docx") return Docx;
            if (head.Length >= 8 && head[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }) && ext == ".doc") return Doc;
            return null;
        }
    }

    /// <summary>The private store for CVs (Infrastructure: a folder on the server).</summary>
    public interface ICvStore
    {
        /// <summary>Keeps the file under a generated name and returns its path in the store.</summary>
        Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);

        /// <summary>The file, or null when it is missing.</summary>
        Stream? OpenRead(string storedPath);

        void Delete(string storedPath);
    }

    /// <summary>Hands a saved application to the email sender.</summary>
    public interface IApplicationNotificationQueue
    {
        void Enqueue(long applicationId);
    }

    /// <summary>What the application form posts (the CV comes separately).</summary>
    public sealed class ApplicationInput
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LinkedIn { get; set; }
        public string? Message { get; set; }
        public bool Consent { get; set; }

        /// <summary>The hidden field people never see; bots fill it in.</summary>
        public string? Website { get; set; }
    }

    /// <summary>The uploaded CV: a seekable stream (ASP.NET Core buffers form files), its name and size.</summary>
    public sealed record CvUpload(Stream Content, string FileName, long Length);

    public static partial class ApplicationRules
    {
        public const int MessageMaxLength = 3000;

        /// <summary>Field keys with a problem: name, email, phone, linkedin, message, cv, cvType, cvSize, consent.</summary>
        public static async Task<(List<string> Errors, CvFileType? Type)> ValidateAsync(ApplicationInput input, CvUpload? cv, long maxCvBytes,
            CancellationToken ct = default)
        {
            var errors = new List<string>();
            if (EnquiryRules.Trim(input.Name).Length is < 2 or > 150) errors.Add("name");
            if (EnquiryRules.Trim(input.Email) is var email && (email.Length > 256 || !EmailPattern().IsMatch(email))) errors.Add("email");
            if (EnquiryRules.Trim(input.Phone) is { Length: > 0 } phone && (phone.Length > 40 || !PhonePattern().IsMatch(phone))) errors.Add("phone");
            if (EnquiryRules.Trim(input.LinkedIn) is { Length: > 0 } linkedIn
                && (linkedIn.Length > 300 || !Uri.TryCreate(linkedIn, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
                errors.Add("linkedin");
            if (EnquiryRules.Trim(input.Message).Length > MessageMaxLength) errors.Add("message");
            if (!input.Consent) errors.Add("consent");

            CvFileType? type = null;
            if (cv is null || cv.Length == 0) errors.Add("cv");
            else if (cv.Length > maxCvBytes) errors.Add("cvSize");
            else
            {
                var head = new byte[8];
                var read = await cv.Content.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
                cv.Content.Position = 0;
                type = CvFileType.Detect(head.AsSpan(0, read), cv.FileName);
                if (type is null) errors.Add("cvType");
            }
            return (errors, type);
        }

        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
        private static partial Regex EmailPattern();

        [GeneratedRegex(@"^[+0-9][0-9\s()\-]{6,}$")]
        private static partial Regex PhonePattern();
    }

    public enum ApplicationOutcome
    {
        Saved,
        Invalid,

        /// <summary>The honeypot was filled: answered as a success, nothing kept.</summary>
        Ignored,

        /// <summary>The role is not open (any more).</summary>
        RoleClosed
    }

    public sealed record ApplicationResult(ApplicationOutcome Outcome, IReadOnlyList<string> Errors, long? ApplicationId = null);

    /// <summary>Checks and saves an application with its CV, then queues the notification email.</summary>
    public sealed class JobApplicationService
    {
        private readonly IContentStore _store;
        private readonly ICvStore _cvs;
        private readonly IApplicationNotificationQueue _queue;
        private readonly CareersOptions _options;
        private readonly EnquiryOptions _enquiryOptions;
        private readonly TimeProvider _clock;

        public JobApplicationService(IContentStore store, ICvStore cvs, IApplicationNotificationQueue queue, IOptions<CareersOptions> options,
            IOptions<EnquiryOptions> enquiryOptions, TimeProvider clock)
        {
            _store = store;
            _cvs = cvs;
            _queue = queue;
            _options = options.Value;
            _enquiryOptions = enquiryOptions.Value;
            _clock = clock;
        }

        public long MaxCvBytes => _options.MaxCvBytes;

        /// <summary>An open role by its address, or null.</summary>
        public async Task<CareerRole?> OpenRoleAsync(string slug, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
            var roles = await _store.ListAsync<CareerRole>(r => r.Slug == slug && r.IsPublished && r.IsActive, ct);
            return roles.FirstOrDefault(r => r.IsOpenOn(today));
        }

        /// <param name="consentText">The consent wording the form showed (plain text).</param>
        public async Task<ApplicationResult> SubmitAsync(string roleSlug, ApplicationInput input, CvUpload? cv, string culture, string consentText,
            string? ipAddress, string? userAgent, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(input.Website)) return new ApplicationResult(ApplicationOutcome.Ignored, []);
            var role = await OpenRoleAsync(roleSlug, ct);
            if (role is null || !string.IsNullOrWhiteSpace(role.ApplyUrl)) return new ApplicationResult(ApplicationOutcome.RoleClosed, []);

            var (errors, type) = await ApplicationRules.ValidateAsync(input, cv, _options.MaxCvBytes, ct);
            if (errors.Count > 0) return new ApplicationResult(ApplicationOutcome.Invalid, errors);

            var stored = await _cvs.SaveAsync(cv!.Content, type!.Extension, ct);
            try
            {
                var lang = culture == "en" ? "en" : "ar";
                var phone = EnquiryRules.Trim(input.Phone);
                var linkedIn = EnquiryRules.Trim(input.LinkedIn);
                var message = EnquiryRules.Trim(input.Message).Replace("\r\n", "\n");
                var application = new JobApplication
                {
                    CareerRoleId = role.Id,
                    RoleTitle = Cut(role.Title.For(lang), 200),
                    Culture = lang,
                    Name = EnquiryRules.Trim(input.Name),
                    Email = EnquiryRules.Trim(input.Email),
                    Phone = phone.Length == 0 ? null : phone,
                    LinkedInUrl = linkedIn.Length == 0 ? null : linkedIn,
                    Message = message.Length == 0 ? null : message,
                    CvFileName = Cut(CleanFileName(cv.FileName, type.Extension), 260),
                    CvStoredPath = stored,
                    CvContentType = type.ContentType,
                    CvSizeBytes = cv.Length,
                    ConsentAt = _clock.GetUtcNow().UtcDateTime,
                    ConsentText = Cut(consentText, 1000),
                    IpHash = HashIp(ipAddress),
                    UserAgent = userAgent is null ? null : Cut(userAgent, 500)
                };
                _store.Add(application);
                await _store.SaveChangesAsync(ct);
                _queue.Enqueue(application.Id);
                return new ApplicationResult(ApplicationOutcome.Saved, [], application.Id);
            }
            catch
            {
                _cvs.Delete(stored);
                throw;
            }
        }

        private string? HashIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_enquiryOptions.IpHashSalt + "|" + ip.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Cut(string value, int max) => value.Length <= max ? value : value[..max];

        /// <summary>The name without folders or characters that break a download header; keeps the real extension.</summary>
        private static string CleanFileName(string fileName, string extension)
        {
            var name = Path.GetFileNameWithoutExtension(fileName.Replace('\\', '/').Split('/')[^1]);
            name = new string(name.Where(c => !char.IsControl(c) && c is not '"' and not ';').ToArray()).Trim();
            return (name.Length == 0 ? "cv" : name) + extension;
        }
    }
}

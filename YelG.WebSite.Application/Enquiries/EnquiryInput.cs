using System.Text.RegularExpressions;

namespace YelG.WebSite.Application.Enquiries
{
    /// <summary>What the consultation form posts.</summary>
    public sealed class EnquiryInput
    {
        /// <summary>The form's value for "not sure yet" in the service list.</summary>
        public const string NotSure = "not-sure";

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }

        /// <summary>A service key, or <see cref="NotSure"/>.</summary>
        public string? Service { get; set; }

        public string? Message { get; set; }
        public bool Consent { get; set; }

        /// <summary>Set by "discuss your result" in the self-check.</summary>
        public int? SelfCheckScore { get; set; }

        /// <summary>The hidden field people never see; bots fill it in.</summary>
        public string? Website { get; set; }
    }

    /// <summary>Where the request came from, for the abuse trail.</summary>
    public sealed record EnquiryOrigin(string Culture, string? IpAddress, string? UserAgent);

    /// <summary>
    /// The form's rules, the same as the page script (and the approved design's validate()): field names are
    /// the keys of the validation messages ("name", "email", …).
    /// </summary>
    public static partial class EnquiryRules
    {
        public const int MessageMinLength = 20;

        public static IReadOnlyList<string> Validate(EnquiryInput input, IReadOnlySet<string> serviceKeys)
        {
            var errors = new List<string>();
            if (Trim(input.Name).Length is < 2 or > 150) errors.Add("name");
            if (Trim(input.Email) is var email && (email.Length > 256 || !EmailPattern().IsMatch(email))) errors.Add("email");
            if (Trim(input.Company).Length is < 2 or > 200) errors.Add("company");
            if (Trim(input.Phone) is { Length: > 0 } phone && (phone.Length > 40 || !PhonePattern().IsMatch(phone))) errors.Add("phone");
            var service = Trim(input.Service);
            if (service != EnquiryInput.NotSure && !serviceKeys.Contains(service)) errors.Add("service");
            if (Trim(input.Message).Length is < MessageMinLength or > 5000) errors.Add("message");
            if (!input.Consent) errors.Add("consent");
            return errors;
        }

        public static string Trim(string? value) => (value ?? "").Trim();

        // the design's patterns: /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/ and /^[+0-9][0-9\s()\-]{6,}$/
        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
        private static partial Regex EmailPattern();

        [GeneratedRegex(@"^[+0-9][0-9\s()\-]{6,}$")]
        private static partial Regex PhonePattern();
    }
}

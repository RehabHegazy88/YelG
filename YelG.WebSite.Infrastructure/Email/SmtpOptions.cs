namespace YelG.WebSite.Infrastructure.Email
{
    /// <summary>
    /// "Smtp" configuration for the company's mail server (mailcow). The password comes from user-secrets or
    /// an environment variable (Smtp__Password), never from a committed file.
    /// </summary>
    public sealed class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;

        /// <summary>587 with STARTTLS (mailcow's submission port), or 465 with SslOnConnect.</summary>
        public int Port { get; set; } = 587;

        /// <summary>StartTls, SslOnConnect, or None (local test servers only).</summary>
        public string Security { get; set; } = "StartTls";

        public string? UserName { get; set; }

        public string? Password { get; set; }

        /// <summary>The sender, a mailbox on the company domain, e.g. "website@yelgadvisory.com".</summary>
        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "YelG Advisory Website";

        public int TimeoutSeconds { get; set; } = 30;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }
}

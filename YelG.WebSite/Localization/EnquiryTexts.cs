using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Localization
{
    /// <summary>
    /// The form's answers after sending. The wording is the company's to set (consultation section in the admin
    /// panel); until it does, these neutral sentences are shown. They are not from the approved design and
    /// need the company's approval.
    /// </summary>
    public static class EnquiryTexts
    {
        private static readonly Dictionary<string, (string Ar, string En)> Pending = new()
        {
            ["successTitle"] = ("تم استلام طلبك.", "We have received your enquiry."),
            ["successText"] = ("سنتواصل معك قريبًا على البريد الذي أدخلته.", "We will be in touch soon at the email address you gave."),
            ["failure"] = ("تعذّر إرسال طلبك الآن. حاول مرة أخرى بعد قليل، أو راسلنا مباشرة على البريد.",
                "We could not send your enquiry just now. Please try again shortly, or email us directly.")
        };

        public static string SuccessTitle(ConsultSection section, string lang) => Or(section.SuccessTitle.For(lang), "successTitle", lang);

        public static string SuccessText(ConsultSection section, string lang) => Or(section.SuccessText.For(lang), "successText", lang);

        public static string Failure(ConsultSection section, string lang) => Or(section.FailureText.For(lang), "failure", lang);

        private static string Or(string value, string key, string lang) =>
            !string.IsNullOrWhiteSpace(value) ? value : lang == SiteLanguage.English ? Pending[key].En : Pending[key].Ar;
    }
}

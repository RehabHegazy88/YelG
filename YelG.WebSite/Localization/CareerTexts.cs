using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Localization
{
    /// <summary>
    /// Wording of the careers pages and the application form. These pages are not in the approved design, so
    /// the labels live here (Arabic, English); the texts the company may want to word itself (form title,
    /// consent, success message, "no openings") are edited in the admin panel's careers screen and fall back to
    /// these until then. All need the company's approval.
    /// </summary>
    public static class CareerTexts
    {
        private static readonly Dictionary<string, (string Ar, string En)> Texts = new()
        {
            ["pageTitle"] = ("الوظائف", "Careers"),
            ["openRoles"] = ("الوظائف المتاحة", "Open positions"),
            ["allDepartments"] = ("كل الأقسام", "All departments"),
            ["viewRole"] = ("تفاصيل الوظيفة", "View role"),
            ["viewAll"] = ("كل الوظائف", "All open positions"),
            ["backToList"] = ("كل الوظائف", "All positions"),
            ["about"] = ("عن الوظيفة", "About the role"),
            ["responsibilities"] = ("المسؤوليات", "Responsibilities"),
            ["requirements"] = ("المتطلبات", "Requirements"),
            ["posted"] = ("تاريخ النشر", "Posted"),
            ["closing"] = ("آخر موعد للتقديم", "Apply by"),
            ["location"] = ("المكان", "Location"),
            ["experience"] = ("الخبرة", "Experience"),
            ["department"] = ("القسم", "Department"),
            ["type"] = ("نوع الدوام", "Employment type"),
            ["applyNow"] = ("قدّم الآن", "Apply now"),
            ["applyExternal"] = ("التقديم على الوظيفة", "Apply for this role"),
            ["applyTitle"] = ("قدّم على هذه الوظيفة", "Apply for this role"),
            ["linkedIn"] = ("رابط LinkedIn", "LinkedIn profile"),
            ["linkedInPlaceholder"] = ("https://www.linkedin.com/in/…", "https://www.linkedin.com/in/…"),
            ["cv"] = ("السيرة الذاتية", "CV"),
            ["cvHint"] = ("PDF أو Word، حتى {max}.", "PDF or Word, up to {max}."),
            ["coverLetter"] = ("رسالة تعريفية", "Cover note"),
            ["submit"] = ("إرسال الطلب", "Send application"),
            ["consent"] = ("أوافق على أن تستخدم YelG Advisory بياناتي وسيرتي الذاتية لتقييم طلبي فقط، وأن تحتفظ بها مدة لا تزيد على 12 شهرًا.",
                "I agree that YelG Advisory may use my details and CV only to assess my application, and keep them for no longer than 12 months."),
            ["successTitle"] = ("تم استلام طلبك.", "We have received your application."),
            ["successText"] = ("شكرًا لاهتمامك بالانضمام إلى YelG Advisory. سنراجع طلبك ونتواصل معك إذا كان مناسبًا.",
                "Thank you for your interest in YelG Advisory. We will review your application and contact you if it is a fit."),
            ["failure"] = ("تعذّر إرسال طلبك الآن. حاول مرة أخرى بعد قليل.", "We could not send your application just now. Please try again shortly."),
            ["closed"] = ("لم تعد هذه الوظيفة متاحة للتقديم.", "This position is no longer open for applications."),
            ["noOpenings"] = ("نعلن هنا عن كل وظيفة جديدة فور فتحها. ويمكنك الآن إرسال سيرتك الذاتية لنتواصل معك عند توفر فرصة مناسبة.",
                "We post every new role here as soon as it opens. You can send us your CV now and we will contact you when a suitable role opens."),
            ["sendCv"] = ("أرسل سيرتك الذاتية", "Send us your CV"),
            ["generalApplication"] = ("طلب عام", "General application"),
            ["generalEyebrow"] = ("طلب عام", "Open application"),
            ["generalTitle"] = ("لم تجد الوظيفة المناسبة؟", "Not seeing the right role?"),
            ["generalText"] = ("أرسل سيرتك الذاتية وسنحتفظ بها، ونتواصل معك عندما تتوفر فرصة تناسب خبرتك.",
                "Send us your CV. We will keep it on file and contact you when a role that fits your experience opens."),
            ["generalStep1"] = ("أرسل بياناتك وسيرتك الذاتية", "Send your details and CV"),
            ["generalStep2"] = ("نراجعها مع فريق التوظيف", "Our team reviews it"),
            ["generalStep3"] = ("نتواصل معك عند توفر فرصة مناسبة", "We contact you when a suitable role opens"),
            ["formTitleGeneral"] = ("أرسل سيرتك الذاتية", "Send us your CV"),
            ["valuesTitle"] = ("ما نؤمن به", "What we stand for"),
            ["noOpeningsTitle"] = ("لا توجد وظائف متاحة الآن", "No open positions right now"),
            // the form's messages (name, email and phone reuse the consultation form's)
            ["err.linkedin"] = ("يرجى إدخال رابط صحيح يبدأ بـ https://، أو ترك الحقل فارغًا.", "Please enter a link starting with https://, or leave this blank."),
            ["err.message"] = ("الرسالة أطول من المسموح (3000 حرف).", "The note is too long (3000 characters at most)."),
            ["err.cv"] = ("يرجى إرفاق سيرتك الذاتية.", "Please attach your CV."),
            ["err.cvType"] = ("يرجى إرفاق ملف PDF أو Word.", "Please attach a PDF or Word file."),
            ["err.cvSize"] = ("حجم الملف أكبر من {max}.", "The file is larger than {max}."),
            ["err.consent"] = ("يرجى الموافقة على استخدام بياناتك لتقييم طلبك.", "Please confirm we may use your details to assess your application."),
            ["type.FullTime"] = ("دوام كامل", "Full-time"),
            ["type.PartTime"] = ("دوام جزئي", "Part-time"),
            ["type.Contract"] = ("عقد مؤقت", "Contract"),
            ["type.Internship"] = ("تدريب", "Internship")
        };

        public static string Get(string key, string lang) =>
            Texts.TryGetValue(key, out var t) ? (lang == SiteLanguage.English ? t.En : t.Ar) : key;

        public static string Type(EmploymentType type, string lang) => Get("type." + type, lang);

        /// <summary>An editable text of the careers section, or the default.</summary>
        public static string Or(LocalizedText? text, string key, string lang) =>
            text?.For(lang) is { Length: > 0 } value && !string.IsNullOrWhiteSpace(value) ? value : Get(key, lang);
    }
}

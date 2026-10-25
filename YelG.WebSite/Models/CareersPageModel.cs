using System.Globalization;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Localization;

namespace YelG.WebSite.Models
{
    /// <summary>The careers page (all open roles) or one role's page with its application form.</summary>
    public sealed class CareersPageModel : HomePageModel
    {
        public CareersPageModel(HomeContent content, string lang, UiStrings ui, string baseUrl, DateOnly today, CareerRole? role,
            string? department, long maxCvBytes, bool preview = false)
            : base(content, lang, ui, preview, baseUrl)
        {
            Today = today;
            Role = role;
            Department = department;
            MaxCvBytes = maxCvBytes;
            OpenRoles = content.CareerRoles.Where(r => r.IsOpenOn(today)).ToList();
        }

        public DateOnly Today { get; }

        /// <summary>The role of a role page; null on the list.</summary>
        public CareerRole? Role { get; }

        /// <summary>The department chosen on the list (its Arabic or English name); null for all.</summary>
        public string? Department { get; }

        public long MaxCvBytes { get; }

        public IReadOnlyList<CareerRole> OpenRoles { get; }

        /// <summary>The departments of the open roles, in this language, for the list's filter.</summary>
        public IReadOnlyList<string> Departments =>
            OpenRoles.Select(r => r.Department.For(Lang)).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().ToList();

        public IReadOnlyList<CareerRole> ListedRoles =>
            string.IsNullOrEmpty(Department) ? OpenRoles : OpenRoles.Where(r => r.Department.For(Lang) == Department).ToList();

        public override string PagePath => Role is null ? "/careers" : $"/careers/{Role.Slug}";

        public override string? PageTitle => Role is null
            ? $"{C("pageTitle")} — YelG Advisory"
            : $"{T(Role.Title)} — {C("pageTitle")} — YelG Advisory";

        public override string? PageDescription => Role is null
            ? (Content.Careers is { } section && !section.Lead.IsEmpty ? T(section.Lead) : null)
            : (!Role.Summary.IsEmpty ? T(Role.Summary) : T(Role.Description).Split('\n')[0]);

        /// <summary>A careers label in this language.</summary>
        public string C(string key) => CareerTexts.Get(key, Lang).Replace("{max}", MaxCvLabel);

        /// <summary>An editable text of the careers section (rendered with its markup), or its default wording.</summary>
        public Microsoft.AspNetCore.Html.IHtmlContent Text(Domain.Common.LocalizedText? editable, string key) =>
            Rendering.RichText.Render(CareerTexts.Or(editable, key, Lang).Replace("{max}", MaxCvLabel),
                new Rendering.RichTextOptions { IsolateLatin = IsArabic });

        public string MaxCvLabel => IsArabic
            ? $"{MaxCvBytes / (1024 * 1024)} ميجابايت"
            : $"{MaxCvBytes / (1024 * 1024)} MB";

        public string Type(EmploymentType type) => CareerTexts.Type(type, Lang);

        /// <summary>"/ar/careers/senior-accountant".</summary>
        public string RoleUrl(CareerRole role) => $"/{Lang}/careers/{role.Slug}";

        public string ListUrl => $"/{Lang}/careers";

        public string Date(DateOnly date) => date.ToString(IsArabic ? "d MMMM yyyy" : "d MMM yyyy", Culture);

        /// <summary>The lines of a "one item per line" text.</summary>
        public IReadOnlyList<string> Lines(Domain.Common.LocalizedText text) =>
            text.For(Lang).Split('\n').Select(l => l.Trim().TrimStart('-', '•', '*').Trim()).Where(l => l.Length > 0).ToList();

        /// <summary>The role as schema.org JobPosting data (for search engines' job listings).</summary>
        public object JobPosting(CareerRole role) => new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "JobPosting",
            ["title"] = T(role.Title),
            ["description"] = string.Join("\n", new[] { T(role.Description), T(role.Responsibilities), T(role.Requirements) }.Where(x => x.Length > 0)),
            ["datePosted"] = role.PostedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["validThrough"] = role.ClosingOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["employmentType"] = role.EmploymentType switch
            {
                EmploymentType.PartTime => "PART_TIME",
                EmploymentType.Contract => "CONTRACTOR",
                EmploymentType.Internship => "INTERN",
                _ => "FULL_TIME"
            },
            ["hiringOrganization"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = "YelG Advisory",
                ["sameAs"] = string.IsNullOrEmpty(Content.Site.WebsiteUrl) ? BaseUrl : Content.Site.WebsiteUrl
            },
            ["jobLocation"] = new Dictionary<string, object?>
            {
                ["@type"] = "Place",
                ["address"] = new Dictionary<string, object?> { ["@type"] = "PostalAddress", ["addressLocality"] = T(role.Location), ["addressCountry"] = "EG" }
            },
            ["url"] = Url(Lang)
        };
    }
}

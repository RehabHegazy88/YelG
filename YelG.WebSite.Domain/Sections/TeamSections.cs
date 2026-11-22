using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Domain.Sections
{
    /// <summary>"Meet our team": the heading and a figure, then the team as one list of cards (the founder first).</summary>
    public class TeamSection : SectionBase
    {
        /// <summary>The figure next to the team ("100+") and what it counts.</summary>
        public string StatValue { get; set; } = string.Empty;
        public LocalizedText StatLabel { get; set; } = new();
    }

    /// <summary>A practice ("المحاسبة"), named on the cards of its members.</summary>
    public class TeamPractice : ContentItem
    {
        public LocalizedText Title { get; set; } = new();
    }

    /// <summary>A team member, shown on their practice and with the whole team.</summary>
    public class TeamMember : ContentItem
    {
        public LocalizedText Name { get; set; } = new();
        public LocalizedText Role { get; set; } = new();

        /// <summary>One or two sentences.</summary>
        public LocalizedText Bio { get; set; } = new();

        /// <summary>Certificates as written, comma separated ("CMA, CFA").</summary>
        public string Credentials { get; set; } = string.Empty;

        /// <summary>A portrait; cropped to a circle and a rounded card.</summary>
        public long? PhotoId { get; set; }
        public MediaAsset? Photo { get; set; }

        public string? LinkedInUrl { get; set; }

        /// <summary>The practice the member sits on; without one the member shows with the whole team only.</summary>
        public long? TeamPracticeId { get; set; }
        public TeamPractice? Practice { get; set; }

        public IReadOnlyList<string> CredentialList() =>
            Credentials.Split(new[] { ',', '،', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Up to two initials for a member without a photo ("مح", "MA").</summary>
        public static string Initials(string name)
        {
            // titles such as "Dr." and "د." are skipped
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => char.IsLetter(w[0]) && !w.EndsWith('.')).ToList();
            // the Arabic article: "الجمل" gives "ج"
            static string First(string w) => (w.StartsWith("ال", StringComparison.Ordinal) && w.Length > 3 ? w[2] : w[0]).ToString();
            return words.Count switch
            {
                0 => "",
                1 => First(words[0]),
                _ => First(words[0]) + First(words[^1])
            };
        }
    }
}

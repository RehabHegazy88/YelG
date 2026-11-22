using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Models
{
    /// <summary>A team member's card; <paramref name="Practice"/> is named on the card in the whole-team panel.</summary>
    public sealed record TeamCardModel(HomePageModel Page, TeamMember Member, TeamPractice? Practice);
}

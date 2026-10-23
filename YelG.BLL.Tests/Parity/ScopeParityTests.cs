using YelG.BLL.Scoring;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Parity
{
    public class ScopeParityTests
    {
        [Theory]
        [MemberData(nameof(ParityCases.Names), MemberType = typeof(ParityCases))]
        public void Scope_matches_the_Apps_Script_generateCompanyScope_function(string caseName)
        {
            var parityCase = ParityCases.Get(caseName);
            var expected = parityCase.Expected.Scope;
            var (templates, rules) = ParityCases.TaskLibrary();
            var model = ParityCases.ModelFor(parityCase);

            var states = parityCase.OptionalIncluded
                .Select(code => templates.Single(t => t.Code == code))
                .ToDictionary(t => (t.Id, true), t => new ScopeItemState(ScopeItemStatus.Planned, t.DefaultOwner, null, null, true));

            var actual = ScopeEngine.Generate(model, ParityCases.AnswersFor(parityCase), templates, rules, states);

            Assert.Equal(expected.Tasks.Select(t => t.Code), actual.Tasks.Select(t => t.Template.Code));
            for (var i = 0; i < expected.Tasks.Count; i++)
            {
                var e = expected.Tasks[i];
                var a = actual.Tasks[i];
                Assert.Equal(e.Priority, a.Priority);
                Assert.Equal(e.Sources, a.Sources);
                Assert.Equal(e.RoleHours.Analyst, a.RoleHours.Analyst);
                Assert.Equal(e.RoleHours.TeamLead, a.RoleHours.TeamLead);
                Assert.Equal(e.RoleHours.Manager, a.RoleHours.Manager);
                Assert.Equal(e.RoleHours.Partner, a.RoleHours.Partner);
                Assert.Equal(e.Hours, a.Hours);
                Assert.Equal(e.VolumeFactor, a.VolumeFactor);
                Assert.Equal(e.VolumeLabel, a.VolumeLabel);
                Assert.Equal(e.AnnualMultiplier, a.AnnualMultiplier);
                Assert.Equal(e.AnnualHours, a.AnnualHours);
                Assert.Equal(e.Owner, a.State.Owner);
                Assert.Equal(e.Status, a.State.Status);
            }

            Assert.Equal(
                expected.Recommendations.Select(r => (r.Code, r.Text, r.Priority, r.Source)),
                actual.Recommendations.Select(r => (r.RuleCode, r.Text, r.Priority, r.Source)));

            Assert.Equal(
                expected.OptionalServices.Select(o => (o.RuleCode, o.TemplateCode, o.Included, o.Source)),
                actual.OptionalServices.Select(o => (o.Rule.Code, o.Template.Code, o.State.OptionalIncluded, o.Source)));

            Assert.Equal(expected.ScoreEffects, actual.ScoreEffects.Select(e => e.Rule.Code));
        }
    }
}

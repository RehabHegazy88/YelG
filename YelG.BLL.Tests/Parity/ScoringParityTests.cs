using YelG.BLL.Scoring;

namespace YelG.BLL.Tests.Parity
{
    public class ScoringParityTests
    {
        private const double Tolerance = 1e-9;

        [Theory]
        [MemberData(nameof(ParityCases.Names), MemberType = typeof(ParityCases))]
        public void Score_matches_the_Apps_Script_score_function(string caseName)
        {
            var parityCase = ParityCases.Get(caseName);
            var expected = parityCase.Expected.Score;

            var actual = ScoringEngine.Score(ParityCases.ModelFor(parityCase), parityCase.ActivityId, ParityCases.AnswersFor(parityCase));

            AssertClose(expected.Confidence, actual.Confidence, "confidence");
            Assert.Equal(expected.Grade, actual.Grade);
            Assert.Equal(expected.Answered, actual.Answered);
            Assert.Equal(expected.VisibleCount, actual.VisibleCount);
            Assert.Equal(expected.Breaches, actual.Breaches.Select(q => q.Code));

            Assert.Equal(expected.Categories.Count, actual.Categories.Count);
            for (var i = 0; i < expected.Categories.Count; i++)
            {
                var e = expected.Categories[i];
                var a = actual.Categories[i];
                Assert.Equal(e.Code, a.Code);
                AssertClose(e.Confidence, a.Confidence, e.Code + " confidence");
                AssertClose(e.Workflow, a.Workflow, e.Code + " workflow");
                AssertClose(e.Pricing, a.Pricing, e.Code + " pricing");
                AssertClose(e.RelativeWeight, a.RelativeWeight, e.Code + " relative weight");
            }
        }

        private static void AssertClose(double? expected, double? actual, string what)
        {
            if (expected is null || actual is null)
            {
                Assert.True(expected is null && actual is null, $"{what}: expected {expected?.ToString() ?? "null"}, got {actual?.ToString() ?? "null"}");
                return;
            }
            Assert.True(Math.Abs(expected.Value - actual.Value) <= Tolerance, $"{what}: expected {expected}, got {actual}");
        }
    }
}

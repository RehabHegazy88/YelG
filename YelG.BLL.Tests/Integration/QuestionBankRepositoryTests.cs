using Microsoft.EntityFrameworkCore;
using YelG.BLL.Repository.QuestionBank;
using YelG.BLL.Scoring;
using YelG.BLL.Tests.Parity;
using YelG.DAL.Data;

namespace YelG.BLL.Tests.Integration
{
    /// <summary>Runs only when YELG_IT_CONNECTION points to a seeded YelgLens database.</summary>
    public sealed class DatabaseFactAttribute : FactAttribute
    {
        public const string Variable = "YELG_IT_CONNECTION";

        public DatabaseFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
                Skip = $"Set {Variable} to run against a real database.";
        }
    }

    public class QuestionBankRepositoryTests
    {
        private static YelgDbContext NewContext() =>
            new(new DbContextOptionsBuilder<YelgDbContext>()
                .UseSqlServer(Environment.GetEnvironmentVariable(DatabaseFactAttribute.Variable))
                .Options);

        [DatabaseFact]
        public async Task Published_version_from_the_database_scores_like_the_Apps_Script()
        {
            await using var context = NewContext();
            var version = await new QuestionBankRepository(context).GetPublishedVersionAsync();
            Assert.NotNull(version);

            var model = version!.ToScoringModel();
            Assert.Equal(154, model.QuestionsByCode.Count);
            Assert.Equal("A03", model.QuestionsByCode["A04"].ParentCode);

            // Cases that use the default (equal) weights must give the golden numbers from the DB model too.
            foreach (var name in ParityCases.Names().Select(n => (string)n[0]))
            {
                var parityCase = ParityCases.Get(name);
                if (parityCase.CatWeights is not null || parityCase.Factors is not null) continue;

                var actual = ScoringEngine.Score(model, parityCase.ActivityId, ParityCases.AnswersFor(parityCase));
                var expected = parityCase.Expected.Score;

                Assert.True(Math.Abs(expected.Confidence - actual.Confidence) <= 1e-9, $"{name}: {expected.Confidence} vs {actual.Confidence}");
                Assert.Equal(expected.Grade, actual.Grade);
                Assert.Equal(expected.Answered, actual.Answered);
                Assert.Equal(expected.VisibleCount, actual.VisibleCount);
                Assert.Equal(expected.Breaches, actual.Breaches.Select(q => q.Code));
            }
        }
    }
}

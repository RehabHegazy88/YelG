using YelG.BLL.Scoring;
using YelG.Model.Enums;

namespace YelG.BLL.Tests
{
    public class ScoringEngineTests
    {
        private static ScoringQuestion YesNo(string code, string? parent = null, string? when = null) => new()
        {
            Code = code,
            Text = code,
            AnswerType = AnswerType.YesNo,
            Kind = QuestionKind.Binary,
            Weights = new[] { 3d, 2d, 1d },
            ParentCode = parent,
            ConditionOperator = parent is null ? null : ConditionOperator.Equals,
            ConditionValue = when,
            Options = new[] { new ScoringOption("نعم", 1), new ScoringOption("لا", 0), new ScoringOption("غير منطبق", null) }
        };

        private static ScoringQuestion Numeric(string code, params ScoringBand[] bands) => new()
        {
            Code = code,
            Text = code,
            AnswerType = AnswerType.Number,
            Kind = QuestionKind.Numeric,
            Weights = new[] { 2d, 1d, 3d },
            Bands = bands
        };

        private static ScoringModel ModelOf(params ScoringQuestion[] questions) =>
            new(1, new[] { new ScoringCategory("أ", "فئة", 100, new Dictionary<long, double>(), questions) });

        private static AnswerSet Answers(params (string Code, string Value)[] values) =>
            new(values.ToDictionary(v => v.Code, v => (IReadOnlyList<string>)new[] { v.Value }));

        [Theory]
        [InlineData("٥", 1.0)]
        [InlineData("15", 0.5)]
        [InlineData("١٢٬٠٠٠", 0.0)]
        public void Numeric_answer_is_scored_by_its_band(string answer, double expected)
        {
            var question = Numeric("N1",
                new ScoringBand(null, 10, 1),
                new ScoringBand(10, 100, 0.5),
                new ScoringBand(100, null, 0));

            Assert.Equal(expected, ScoringEngine.ValueOf(question, Answers(("N1", answer))));
        }

        [Fact]
        public void Numeric_answer_without_bands_is_reported_as_unscored()
        {
            var model = ModelOf(YesNo("Q1"), Numeric("N1"));

            var score = ScoringEngine.Score(model, 1, Answers(("Q1", "نعم"), ("N1", "250")));

            Assert.Equal(100d, score.Confidence);
            Assert.Equal(new[] { "N1" }, score.Unscored.Select(q => q.Code));
        }

        [Fact]
        public void Not_applicable_answer_is_not_reported_as_unscored()
        {
            var score = ScoringEngine.Score(ModelOf(YesNo("Q1")), 1, Answers(("Q1", "غير منطبق")));

            Assert.Empty(score.Unscored);
            Assert.Equal(1, score.Answered);
        }

        [Fact]
        public void Text_without_a_number_does_not_match_a_band()
        {
            var question = Numeric("N1", new ScoringBand(null, null, 1));

            Assert.Null(ScoringEngine.ValueOf(question, Answers(("N1", "لا يوجد"))));
        }

        [Fact]
        public void Answers_of_hidden_questions_are_dropped_before_scope_rules()
        {
            var model = ModelOf(YesNo("P1"), YesNo("C1", parent: "P1", when: "نعم"));
            var answers = Answers(("P1", "لا"), ("C1", "نعم"));

            var visible = ScopeEngine.VisibleAnswers(model, answers);

            Assert.True(visible.IsAnswered("P1"));
            Assert.False(visible.IsAnswered("C1"));
        }
    }
}

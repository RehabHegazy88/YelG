using YelG.BLL.Scoring;
using YelG.Model.Enums;
using YelG.Model.Scope;

namespace YelG.BLL.Tests
{
    public class ScopeEngineTests
    {
        [Theory]
        [InlineData("١٢٬٥٠٠٬٠٠٠", 12500000d)] // Arabic thousands separator
        [InlineData("٣٫٥ مليون", 3.5d)]        // Arabic decimal separator
        [InlineData("12,500,000", 12500000d)]
        [InlineData("٣ مخازن", 3d)]
        [InlineData("۷ خزن", 7d)]
        [InlineData("حوالي 40.5 تقريبا", 40.5d)]
        [InlineData("-3 فرق", -3d)]
        [InlineData("لا يوجد", 0d)]
        [InlineData("", 0d)]
        public void NumericValue_reads_the_first_number(string text, double expected)
        {
            Assert.Equal(expected, ScopeEngine.NumericValue(text));
        }

        [Fact]
        public void Price_is_null_until_a_rate_is_set()
        {
            var hours = new RoleHours { TeamLead = 10 };

            Assert.Null(ScopeEngine.Price(hours, new PricingSettings()));

            var settings = new PricingSettings { HourlyRates = new RoleHours { TeamLead = 500 }, MarginPct = 20 };
            Assert.Equal(6000m, ScopeEngine.Price(hours, settings));
        }

        [Theory]
        [InlineData(TaskFrequency.Weekly, 52)]
        [InlineData(TaskFrequency.BiWeekly, 26)]
        [InlineData(TaskFrequency.Monthly, 12)]
        [InlineData(TaskFrequency.Quarterly, 4)]
        [InlineData(TaskFrequency.SemiAnnual, 2)]
        [InlineData(TaskFrequency.Annual, 1)]
        [InlineData(TaskFrequency.OneTime, 1)]
        [InlineData(TaskFrequency.AsNeeded, 1)]
        public void Frequency_multiplier_matches_the_front_end(TaskFrequency frequency, int expected)
        {
            Assert.Equal(expected, ScopeEngine.FrequencyMultiplier(frequency));
        }
    }
}

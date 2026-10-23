using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;

namespace YelG.Model.Scope
{
    /// <summary>Single row: hourly rate per role and the pricing margin (WORKLOAD_SETTINGS).</summary>
    public class PricingSettings : MainBaseEntity<User>
    {
        public RoleHours HourlyRates { get; set; } = new();
        public decimal MarginPct { get; set; }
        public BasisStatus MethodologyStatus { get; set; } = BasisStatus.Draft;
        public string? MethodologyNote { get; set; }
    }
}

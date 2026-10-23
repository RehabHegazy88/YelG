namespace YelG.Model.Scope
{
    /// <summary>Hours (or hourly rates) per team role. Mapped as an owned type.</summary>
    public class RoleHours
    {
        public decimal Analyst { get; set; }
        public decimal TeamLead { get; set; }
        public decimal Manager { get; set; }
        public decimal Partner { get; set; }

        public decimal Total => Analyst + TeamLead + Manager + Partner;
    }
}

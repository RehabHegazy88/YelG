namespace YelG.BLL.ViewModel
{
    /// <summary>Dates are Cairo calendar days, both inclusive. Group is one of ActivityActions.Groups.</summary>
    public sealed record ActivityLogFilter(DateOnly? From, DateOnly? To, long? UserId, string? Group, string? Search, int Page = 1);

    public sealed record ActivityLogRow(
        long Id, DateTimeOffset OccurredAt, long? UserId, string UserName,
        string Action, string ActionLabel, string Group, string? Details);

    public sealed record ActivityLogStats(int Today, int ActiveUsersLast7Days, int FailedLoginsLast7Days, int ExportsLast30Days);

    public sealed record UserChoice(long Id, string DisplayName);

    public sealed record ActivityLogPage(
        IReadOnlyList<ActivityLogRow> Rows, int Total, int Page, int PageSize,
        ActivityLogStats Stats, IReadOnlyList<UserChoice> Users)
    {
        public int PageCount => Math.Max(1, (Total + PageSize - 1) / PageSize);
    }
}

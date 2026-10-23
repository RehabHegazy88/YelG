using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    /// <param name="IsOptionalService">True to switch an optional service in or out (OptionalIncluded).</param>
    public sealed record UpdateScopeItemRequest(
        long TaskTemplateId,
        bool IsOptionalService,
        ScopeItemStatus Status,
        TaskOwner Owner,
        DateOnly? DueDate,
        string? EvidenceNote,
        bool OptionalIncluded);
}

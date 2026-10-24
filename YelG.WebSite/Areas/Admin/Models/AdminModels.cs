using System.ComponentModel.DataAnnotations;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Areas.Admin.Models
{
    public sealed class MediaLibraryModel
    {
        public required IReadOnlyList<MediaAsset> Items { get; init; }
        public MediaKind? Kind { get; init; }
        public string? Query { get; init; }
        public int Page { get; init; }
        public int PageCount { get; init; }
        public int Total { get; init; }
        public required string MaxImage { get; init; }
        public required string MaxVideo { get; init; }
    }

    public sealed class MediaDetailsModel
    {
        public required MediaAsset Asset { get; init; }
        public EditErrors Errors { get; init; } = new();

        /// <summary>Where the file is used: the section name and its screen.</summary>
        public IReadOnlyList<(string Name, string? Url)> Usages { get; init; } = [];
    }

    public sealed class DashboardModel
    {
        public required IReadOnlyList<DashboardTile> Tiles { get; init; }
        public IReadOnlyList<AuditLog> Recent { get; init; } = [];
        public int MediaCount { get; init; }
        public required EditorRegistry Registry { get; init; }
    }

    /// <summary>A screen on the dashboard: whether its section is shown, how many items it has, when it last changed.</summary>
    public sealed record DashboardTile(EditorScreen Screen, bool? IsPublished, int Items, DateTime? LastModified);

    public sealed class AuditListModel
    {
        public required AuditLogPage Page { get; init; }
        public required AuditLogFilter Filter { get; init; }
        public required IReadOnlyList<string> EntityTypes { get; init; }
        public required IReadOnlyList<string> Users { get; init; }
        public required EditorRegistry Registry { get; init; }
    }

    public sealed class UserListModel
    {
        public required IReadOnlyList<(AdminUser User, string Role)> Users { get; init; }
        public long CurrentUserId { get; init; }
    }

    public sealed class UserInput
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "أدخل الاسم.")]
        [StringLength(150, ErrorMessage = "الحد الأقصى 150 حرفًا.")]
        [Display(Name = "الاسم")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "أدخل البريد الإلكتروني.")]
        [EmailAddress(ErrorMessage = "أدخل بريدًا إلكترونيًا صحيحًا.")]
        [StringLength(256)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الدور")]
        public string Role { get; set; } = "Editor";

        [Display(Name = "الحساب مفعّل")]
        public bool IsActive { get; set; } = true;

        /// <summary>Only when creating: the temporary password, replaced at the first sign-in.</summary>
        [StringLength(128, MinimumLength = 10, ErrorMessage = "كلمة المرور من ١٠ أحرف على الأقل.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة مرور مؤقتة")]
        public string? Password { get; set; }

        public bool IsLockedOut { get; set; }
        public bool IsSelf { get; set; }
    }

    public sealed class ResetPasswordInput
    {
        [Required(ErrorMessage = "أدخل كلمة مرور مؤقتة.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "كلمة المرور من ١٠ أحرف على الأقل.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة مرور مؤقتة جديدة")]
        public string Password { get; set; } = string.Empty;
    }
}

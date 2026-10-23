using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace YelG.Model.Auth;

/// <summary>
/// أدوار المنظومة وفق FR-23.
/// </summary>
public enum RoleType
{
    Anonymous = 0,

    /// <summary>مدير نظام</summary>
    SystemAdmin = 1,

    /// <summary>مدير الإدارة المركزية</summary>
    Admin = 2,

    /// <summary>رئيس فريق المراجعة</summary>
    TeamLead = 3,

    /// <summary>مراجع</summary>
    Auditor = 4,

    /// <summary>مطلع (قراءة فقط)</summary>
    Viewer = 5

}

[Table("IdentityRoles")]
public class Role : IdentityRole<long>
{
    [Required]
    [DefaultValue(RoleType.Anonymous)]
    public RoleType Type { get; set; }

    public virtual List<UserRole> UserRole { get; set; } = new();
}
